# Client-Side Caching Implementation Plan for Marain.Tenancy.Client

## Executive Summary

This document outlines the implementation plan for adding client-side caching to the Kiota-based Tenancy client. The caching system will respect Cache-Control headers returned from the API, providing intelligent request optimization while maintaining data consistency.

## Current State Analysis

### Existing Architecture
- **Kiota-generated client**: `TenancyApiClient` (auto-generated)
- **Service wrapper**: `TenancyService` implements `ITenancyService`
- **HTTP transport**: Uses `HttpClientRequestAdapter` from Kiota
- **Authentication**: Supports Azure Identity and anonymous authentication
- **DI registration**: Via `TenancyClientServiceCollectionExtensions`

### Current Operations
1. `GetTenantAsync(string tenantId, string? ifNoneMatch = null)`
2. `UpdateTenantAsync(string tenantId, UpdateTenantRequestJsonPatchDocument)`
3. `GetChildTenantsAsync(string tenantId, string? continuationToken, int? maxItems)`
4. `CreateChildTenantAsync(string tenantId, CreateChildTenantRequest)`
5. `DeleteChildTenantAsync(string tenantId, string childTenantId)`

## Implementation Strategy

### Phase 1: Core Caching Infrastructure

#### 1.1 Cache Storage Abstraction

Create a flexible cache storage interface to support multiple storage backends:

```csharp
// Marain/Tenancy/Client/Caching/ICacheStorage.cs
public interface ICacheStorage
{
    Task<CachedResponse?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, CachedResponse response, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

// Marain/Tenancy/Client/Caching/CachedResponse.cs
public class CachedResponse
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public Dictionary<string, string> Headers { get; set; } = new();
    public int StatusCode { get; set; }
    public string? ContentType { get; set; }
    public string? ETag { get; set; }
    public DateTimeOffset CachedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool MustRevalidate { get; set; }
}
```

#### 1.2 Cache Storage Implementations

**In-Memory Cache (Default)**:
```csharp
// Marain/Tenancy/Client/Caching/MemoryCacheStorage.cs
public class MemoryCacheStorage : ICacheStorage
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheStorage> _logger;
    
    public MemoryCacheStorage(IMemoryCache memoryCache, ILogger<MemoryCacheStorage> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }
    
    public Task<CachedResponse?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var response = _memoryCache.Get<CachedResponse>(key);
        return Task.FromResult(response);
    }
    
    public Task SetAsync(string key, CachedResponse response, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry;
        }
        
        _memoryCache.Set(key, response, options);
        return Task.CompletedTask;
    }
    
    // Additional methods...
}
```

**Distributed Cache Support**:
```csharp
// Marain/Tenancy/Client/Caching/DistributedCacheStorage.cs
public class DistributedCacheStorage : ICacheStorage
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DistributedCacheStorage> _logger;
    
    // Implementation using IDistributedCache for Redis, SQL Server, etc.
}
```

#### 1.3 Cache-Control Header Parser

```csharp
// Marain/Tenancy/Client/Caching/CacheControlParser.cs
public class CacheControlParser
{
    public static CacheDirectives Parse(string? cacheControlHeader)
    {
        if (string.IsNullOrWhiteSpace(cacheControlHeader))
            return new CacheDirectives();
            
        var directives = new CacheDirectives();
        var parts = cacheControlHeader.Split(',', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            
            if (trimmed.Equals("no-cache", StringComparison.OrdinalIgnoreCase))
                directives.NoCache = true;
            else if (trimmed.Equals("no-store", StringComparison.OrdinalIgnoreCase))
                directives.NoStore = true;
            else if (trimmed.Equals("must-revalidate", StringComparison.OrdinalIgnoreCase))
                directives.MustRevalidate = true;
            else if (trimmed.StartsWith("max-age=", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(trimmed[8..], out int maxAge))
                    directives.MaxAge = TimeSpan.FromSeconds(maxAge);
            }
            // Handle other directives...
        }
        
        return directives;
    }
}

public class CacheDirectives
{
    public bool NoCache { get; set; }
    public bool NoStore { get; set; }
    public bool MustRevalidate { get; set; }
    public TimeSpan? MaxAge { get; set; }
    public bool Public { get; set; }
    public bool Private { get; set; }
}
```

### Phase 2: Kiota Middleware Integration

#### 2.1 Caching HTTP Handler

```csharp
// Marain/Tenancy/Client/Caching/TenancyCachingHandler.cs
public class TenancyCachingHandler : DelegatingHandler
{
    private readonly ICacheStorage _cacheStorage;
    private readonly ITenancyCacheKeyGenerator _keyGenerator;
    private readonly TenancyCacheOptions _options;
    private readonly ILogger<TenancyCachingHandler> _logger;

    public TenancyCachingHandler(
        ICacheStorage cacheStorage,
        ITenancyCacheKeyGenerator keyGenerator,
        IOptions<TenancyCacheOptions> options,
        ILogger<TenancyCachingHandler> logger)
    {
        _cacheStorage = cacheStorage;
        _keyGenerator = keyGenerator;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Only cache GET requests
        if (request.Method != HttpMethod.Get)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        var cacheKey = _keyGenerator.GenerateKey(request);
        
        // Try to get from cache
        var cachedResponse = await _cacheStorage.GetAsync(cacheKey, cancellationToken);
        
        if (cachedResponse != null && IsValid(cachedResponse))
        {
            // Handle conditional requests (ETag)
            if (ShouldRevalidate(request, cachedResponse))
            {
                request.Headers.IfNoneMatch.Clear();
                if (!string.IsNullOrEmpty(cachedResponse.ETag))
                {
                    request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(cachedResponse.ETag));
                }
            }
            else
            {
                // Return cached response
                _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return CreateResponseFromCache(cachedResponse);
            }
        }

        // Make actual request
        var response = await base.SendAsync(request, cancellationToken);

        // Handle 304 Not Modified
        if (response.StatusCode == HttpStatusCode.NotModified && cachedResponse != null)
        {
            _logger.LogDebug("304 Not Modified - returning cached response for {CacheKey}", cacheKey);
            return CreateResponseFromCache(cachedResponse);
        }

        // Cache successful responses
        if (response.IsSuccessStatusCode)
        {
            await CacheResponse(cacheKey, response, cancellationToken);
        }

        return response;
    }

    private bool IsValid(CachedResponse cachedResponse)
    {
        if (cachedResponse.ExpiresAt.HasValue)
        {
            return DateTimeOffset.UtcNow < cachedResponse.ExpiresAt.Value;
        }
        
        return true; // No expiry set, assume valid
    }

    private bool ShouldRevalidate(HttpRequestMessage request, CachedResponse cachedResponse)
    {
        // Always revalidate if must-revalidate is set and cache is stale
        if (cachedResponse.MustRevalidate && cachedResponse.ExpiresAt.HasValue 
            && DateTimeOffset.UtcNow >= cachedResponse.ExpiresAt.Value)
        {
            return true;
        }

        // Check for force-refresh scenarios
        var cacheControl = request.Headers.CacheControl;
        if (cacheControl?.NoCache == true)
        {
            return true;
        }

        return false;
    }

    private async Task CacheResponse(string cacheKey, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        // Parse Cache-Control header
        var cacheControlHeader = response.Headers.CacheControl?.ToString();
        var directives = CacheControlParser.Parse(cacheControlHeader);

        // Don't cache if no-store is present
        if (directives.NoStore)
        {
            return;
        }

        // Read response content
        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        
        var cachedResponse = new CachedResponse
        {
            Content = content,
            StatusCode = (int)response.StatusCode,
            ContentType = response.Content.Headers.ContentType?.ToString(),
            ETag = response.Headers.ETag?.Tag,
            CachedAt = DateTimeOffset.UtcNow,
            MustRevalidate = directives.MustRevalidate
        };

        // Copy relevant headers
        foreach (var header in response.Headers.Where(h => ShouldCacheHeader(h.Key)))
        {
            cachedResponse.Headers[header.Key] = string.Join(",", header.Value);
        }
        
        // Calculate expiry
        TimeSpan? expiry = null;
        if (directives.MaxAge.HasValue)
        {
            expiry = directives.MaxAge;
            cachedResponse.ExpiresAt = DateTimeOffset.UtcNow.Add(directives.MaxAge.Value);
        }
        else if (response.Content.Headers.Expires.HasValue)
        {
            cachedResponse.ExpiresAt = response.Content.Headers.Expires;
            expiry = cachedResponse.ExpiresAt - DateTimeOffset.UtcNow;
        }

        // Apply default expiry if none specified
        if (!expiry.HasValue && _options.DefaultCacheExpiry.HasValue)
        {
            expiry = _options.DefaultCacheExpiry;
            cachedResponse.ExpiresAt = DateTimeOffset.UtcNow.Add(_options.DefaultCacheExpiry.Value);
        }

        await _cacheStorage.SetAsync(cacheKey, cachedResponse, expiry, cancellationToken);
        _logger.LogDebug("Cached response for {CacheKey} with expiry {Expiry}", cacheKey, expiry);
    }

    private static bool ShouldCacheHeader(string headerName)
    {
        var headersToCache = new[]
        {
            "Content-Type", "Content-Encoding", "Content-Language",
            "Last-Modified", "ETag", "Location"
        };
        
        return headersToCache.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }

    private static HttpResponseMessage CreateResponseFromCache(CachedResponse cachedResponse)
    {
        var response = new HttpResponseMessage((HttpStatusCode)cachedResponse.StatusCode)
        {
            Content = new ByteArrayContent(cachedResponse.Content)
        };

        // Restore headers
        foreach (var header in cachedResponse.Headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                response.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(header.Value);
            }
            else
            {
                response.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return response;
    }
}
```

#### 2.2 Cache Key Generation Strategy

```csharp
// Marain/Tenancy/Client/Caching/ITenancyCacheKeyGenerator.cs
public interface ITenancyCacheKeyGenerator
{
    string GenerateKey(HttpRequestMessage request);
}

// Marain/Tenancy/Client/Caching/TenancyCacheKeyGenerator.cs
public class TenancyCacheKeyGenerator : ITenancyCacheKeyGenerator
{
    private readonly TenancyCacheOptions _options;

    public TenancyCacheKeyGenerator(IOptions<TenancyCacheOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateKey(HttpRequestMessage request)
    {
        var uriBuilder = new StringBuilder();
        uriBuilder.Append(request.Method.Method);
        uriBuilder.Append(':');
        uriBuilder.Append(request.RequestUri?.GetLeftPart(UriPartial.Path));

        // Include relevant query parameters
        if (request.RequestUri?.Query != null)
        {
            var queryParams = ParseQueryString(request.RequestUri.Query)
                .Where(kvp => _options.CacheableQueryParameters.Contains(kvp.Key))
                .OrderBy(kvp => kvp.Key)
                .ToList();

            if (queryParams.Any())
            {
                uriBuilder.Append('?');
                uriBuilder.Append(string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}")));
            }
        }

        // Include relevant headers for cache key
        var relevantHeaders = new List<string>();
        foreach (var headerName in _options.VaryHeaders)
        {
            if (request.Headers.TryGetValues(headerName, out var values))
            {
                relevantHeaders.Add($"{headerName}:{string.Join(",", values)}");
            }
        }

        if (relevantHeaders.Any())
        {
            uriBuilder.Append('|');
            uriBuilder.Append(string.Join("|", relevantHeaders));
        }

        var keyString = uriBuilder.ToString();
        
        // Hash for consistent key length
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        return $"tenancy:{Convert.ToHexString(hashBytes)}";
    }

    private static Dictionary<string, string> ParseQueryString(string queryString)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(queryString) || queryString == "?")
            return result;

        var query = queryString.StartsWith("?") ? queryString[1..] : queryString;
        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }
}
```

### Phase 3: Configuration and Options

#### 3.1 Cache Configuration

```csharp
// Marain/Tenancy/Client/Caching/TenancyCacheOptions.cs
public class TenancyCacheOptions
{
    public const string SectionName = "TenancyClient:Caching";

    /// <summary>
    /// Whether caching is enabled. Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default cache expiry when no Cache-Control headers are present. Default: 5 minutes
    /// </summary>
    public TimeSpan? DefaultCacheExpiry { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum cache entry size in bytes. Default: 1MB
    /// </summary>
    public long MaxCacheEntrySize { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Query parameters to include in cache key generation
    /// </summary>
    public HashSet<string> CacheableQueryParameters { get; set; } = new()
    {
        "continuationToken",
        "maxItems"
    };

    /// <summary>
    /// Headers to include in cache key generation (Vary headers)
    /// </summary>
    public HashSet<string> VaryHeaders { get; set; } = new()
    {
        "Accept",
        "Accept-Language",
        "Authorization"
    };

    /// <summary>
    /// Cache storage type
    /// </summary>
    public CacheStorageType StorageType { get; set; } = CacheStorageType.Memory;

    /// <summary>
    /// Distributed cache configuration (when using distributed storage)
    /// </summary>
    public DistributedCacheOptions? DistributedCache { get; set; }
}

public enum CacheStorageType
{
    Memory,
    Distributed
}

public class DistributedCacheOptions
{
    public string? ConnectionString { get; set; }
    public string? InstanceName { get; set; }
}
```

### Phase 4: Enhanced TenancyService Integration

#### 4.1 Cached TenancyService Wrapper

```csharp
// Marain/Tenancy/Client/CachedTenancyService.cs
public class CachedTenancyService : ITenancyService
{
    private readonly ITenancyService _innerService;
    private readonly ICacheStorage _cacheStorage;
    private readonly ITenancyCacheKeyGenerator _keyGenerator;
    private readonly ILogger<CachedTenancyService> _logger;

    public CachedTenancyService(
        ITenancyService innerService,
        ICacheStorage cacheStorage,
        ITenancyCacheKeyGenerator keyGenerator,
        ILogger<CachedTenancyService> logger)
    {
        _innerService = innerService;
        _cacheStorage = cacheStorage;
        _keyGenerator = keyGenerator;
        _logger = logger;
    }

    public async Task<TenantResponse?> GetTenantAsync(string tenantId, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
    {
        // The caching is handled at the HTTP layer, so we just delegate
        var result = await _innerService.GetTenantAsync(tenantId, ifNoneMatch, cancellationToken);
        return result;
    }

    public async Task<TenantResponse?> UpdateTenantAsync(string tenantId, UpdateTenantRequestJsonPatchDocument patchOperations, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.UpdateTenantAsync(tenantId, patchOperations, cancellationToken);
        
        // Invalidate cache for this tenant after update
        await InvalidateTenantCache(tenantId, cancellationToken);
        
        return result;
    }

    public async Task<ChildTenantsResponse?> GetChildTenantsAsync(string tenantId, string? continuationToken = null, int? maxItems = null, CancellationToken cancellationToken = default)
    {
        return await _innerService.GetChildTenantsAsync(tenantId, continuationToken, maxItems, cancellationToken);
    }

    public async Task<TenantResponse?> CreateChildTenantAsync(string tenantId, CreateChildTenantRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _innerService.CreateChildTenantAsync(tenantId, request, cancellationToken);
        
        // Invalidate parent tenant's children cache
        await InvalidateChildTenantsCache(tenantId, cancellationToken);
        
        return result;
    }

    public async Task DeleteChildTenantAsync(string tenantId, string childTenantId, CancellationToken cancellationToken = default)
    {
        await _innerService.DeleteChildTenantAsync(tenantId, childTenantId, cancellationToken);
        
        // Invalidate both tenant and children cache
        await InvalidateTenantCache(childTenantId, cancellationToken);
        await InvalidateChildTenantsCache(tenantId, cancellationToken);
    }

    private async Task InvalidateTenantCache(string tenantId, CancellationToken cancellationToken)
    {
        var pattern = $"*/{tenantId}/marain/tenant*";
        await _cacheStorage.RemoveByPatternAsync(pattern, cancellationToken);
        _logger.LogDebug("Invalidated cache for tenant {TenantId}", tenantId);
    }

    private async Task InvalidateChildTenantsCache(string tenantId, CancellationToken cancellationToken)
    {
        var pattern = $"*/{tenantId}/marain/tenant/children*";
        await _cacheStorage.RemoveByPatternAsync(pattern, cancellationToken);
        _logger.LogDebug("Invalidated children cache for tenant {TenantId}", tenantId);
    }
}
```

### Phase 5: Dependency Injection Setup

#### 5.1 Enhanced Service Registration

```csharp
// Marain/Tenancy/Client/TenancyClientServiceCollectionExtensions.cs (Updated)
public static class TenancyClientServiceCollectionExtensions
{
    public static IServiceCollection AddTenancyClient(
        this IServiceCollection services,
        string baseUrl,
        TokenCredential? tokenCredential = null,
        Action<TenancyCacheOptions>? configureCaching = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentNullException(nameof(baseUrl));
        }

        // Configure caching options
        if (configureCaching != null)
        {
            services.Configure(configureCaching);
        }
        else
        {
            services.Configure<TenancyCacheOptions>(options => { }); // Use defaults
        }

        // Register caching services
        services.AddMemoryCache();
        services.AddSingleton<ITenancyCacheKeyGenerator, TenancyCacheKeyGenerator>();
        
        // Register cache storage based on configuration
        services.AddSingleton<ICacheStorage>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TenancyCacheOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<MemoryCacheStorage>>();
            
            return options.StorageType switch
            {
                CacheStorageType.Memory => new MemoryCacheStorage(
                    serviceProvider.GetRequiredService<IMemoryCache>(), 
                    logger),
                CacheStorageType.Distributed => new DistributedCacheStorage(
                    serviceProvider.GetRequiredService<IDistributedCache>(),
                    serviceProvider.GetRequiredService<ILogger<DistributedCacheStorage>>()),
                _ => throw new InvalidOperationException($"Unsupported cache storage type: {options.StorageType}")
            };
        });

        // Register HTTP client with caching handler
        services.AddHttpClient();

        // Register authentication provider
        services.AddSingleton<IAuthenticationProvider>(serviceProvider =>
        {
            if (tokenCredential != null)
            {
                return new AzureIdentityAuthenticationProvider(tokenCredential);
            }
            else
            {
                return new AnonymousAuthenticationProvider();
            }
        });

        // Register request adapter with caching middleware
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TenancyCacheOptions>>().Value;
            IAuthenticationProvider authProvider = serviceProvider.GetRequiredService<IAuthenticationProvider>();
            
            // Create HTTP client with caching handler if enabled
            HttpClient httpClient;
            if (options.Enabled)
            {
                var cachingHandler = new TenancyCachingHandler(
                    serviceProvider.GetRequiredService<ICacheStorage>(),
                    serviceProvider.GetRequiredService<ITenancyCacheKeyGenerator>(),
                    serviceProvider.GetRequiredService<IOptions<TenancyCacheOptions>>(),
                    serviceProvider.GetRequiredService<ILogger<TenancyCachingHandler>>())
                {
                    InnerHandler = new HttpClientHandler()
                };

                httpClient = new HttpClient(cachingHandler);
            }
            else
            {
                httpClient = serviceProvider.GetRequiredService<HttpClient>();
            }

            HttpClientRequestAdapter adapter = new(authProvider, httpClient: httpClient);
            adapter.BaseUrl = baseUrl;
            return adapter;
        });

        // Register the Kiota client and service wrapper
        services.AddSingleton<TenancyApiClient>();
        services.AddSingleton<ITenancyService>(serviceProvider =>
        {
            var innerService = new TenancyService(serviceProvider.GetRequiredService<TenancyApiClient>());
            var options = serviceProvider.GetRequiredService<IOptions<TenancyCacheOptions>>().Value;
            
            if (options.Enabled)
            {
                return new CachedTenancyService(
                    innerService,
                    serviceProvider.GetRequiredService<ICacheStorage>(),
                    serviceProvider.GetRequiredService<ITenancyCacheKeyGenerator>(),
                    serviceProvider.GetRequiredService<ILogger<CachedTenancyService>>());
            }
            
            return innerService;
        });

        return services;
    }

    // Overload for distributed caching
    public static IServiceCollection AddTenancyClientWithDistributedCache(
        this IServiceCollection services,
        string baseUrl,
        string connectionString,
        TokenCredential? tokenCredential = null,
        Action<TenancyCacheOptions>? configureCaching = null)
    {
        // Add distributed cache (Redis)
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
        });

        return services.AddTenancyClient(baseUrl, tokenCredential, options =>
        {
            options.StorageType = CacheStorageType.Distributed;
            configureCaching?.Invoke(options);
        });
    }
}
```

### Phase 6: Testing Strategy

#### 6.1 Unit Tests

```csharp
// Tests/Unit/Caching/CacheControlParserTests.cs
[TestFixture]
public class CacheControlParserTests
{
    [Test]
    public void Parse_MaxAgeDirective_ParsesCorrectly()
    {
        var directives = CacheControlParser.Parse("public, max-age=3600");
        
        directives.Public.Should().BeTrue();
        directives.MaxAge.Should().Be(TimeSpan.FromSeconds(3600));
    }

    [Test]
    public void Parse_NoStoreDirective_ParsesCorrectly()
    {
        var directives = CacheControlParser.Parse("no-store, no-cache");
        
        directives.NoStore.Should().BeTrue();
        directives.NoCache.Should().BeTrue();
    }
}

// Tests/Unit/Caching/TenancyCacheKeyGeneratorTests.cs
[TestFixture]
public class TenancyCacheKeyGeneratorTests
{
    [Test]
    public void GenerateKey_SameRequest_GeneratesSameKey()
    {
        var generator = new TenancyCacheKeyGenerator(Options.Create(new TenancyCacheOptions()));
        var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/tenant/123");
        var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/tenant/123");

        var key1 = generator.GenerateKey(request1);
        var key2 = generator.GenerateKey(request2);

        key1.Should().Be(key2);
    }
}
```

#### 6.2 Integration Tests

```csharp
// Tests/Integration/CachingIntegrationTests.cs
[TestFixture]
public class CachingIntegrationTests
{
    private TestServer _server;
    private HttpClient _client;
    private ICacheStorage _cacheStorage;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddLogging();
        
        // Setup test server with caching
        var provider = services.BuildServiceProvider();
        _cacheStorage = new MemoryCacheStorage(
            provider.GetRequiredService<IMemoryCache>(),
            provider.GetRequiredService<ILogger<MemoryCacheStorage>>());
    }

    [Test]
    public async Task GetTenant_WithCacheHeaders_CachesResponse()
    {
        // Setup mock API response with cache headers
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new TenantResponse { Id = "test-tenant" }))
        };
        mockResponse.Headers.CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromMinutes(30)
        };

        // Test caching behavior
        var cachedResponse = await _cacheStorage.GetAsync("test-key");
        cachedResponse.Should().NotBeNull();
    }
}
```

## Implementation Timeline

### Week 1-2: Core Infrastructure
- [ ] Implement `ICacheStorage` interface and `MemoryCacheStorage`
- [ ] Create `CacheControlParser` and `CacheDirectives`
- [ ] Implement `TenancyCacheKeyGenerator`
- [ ] Create `TenancyCacheOptions` configuration

### Week 3-4: HTTP Middleware
- [ ] Implement `TenancyCachingHandler`
- [ ] Add cache invalidation logic
- [ ] Integrate with Kiota request pipeline
- [ ] Implement conditional request handling (ETag support)

### Week 5: Service Integration
- [ ] Create `CachedTenancyService` wrapper
- [ ] Update DI registration extensions
- [ ] Add distributed cache support
- [ ] Configuration and options setup

### Week 6: Testing & Documentation
- [ ] Unit tests for all components
- [ ] Integration tests with mock API
- [ ] Performance benchmarks
- [ ] Update documentation and examples

## Configuration Examples

### Basic Configuration
```csharp
services.AddTenancyClient("https://api.tenancy.com", options =>
{
    options.DefaultCacheExpiry = TimeSpan.FromMinutes(10);
    options.MaxCacheEntrySize = 2 * 1024 * 1024; // 2MB
});
```

### Distributed Cache Configuration
```csharp
services.AddTenancyClientWithDistributedCache(
    "https://api.tenancy.com",
    "localhost:6379", // Redis connection
    options =>
    {
        options.DefaultCacheExpiry = TimeSpan.FromHours(1);
        options.CacheableQueryParameters.Add("customParam");
    });
```

### Disable Caching
```csharp
services.AddTenancyClient("https://api.tenancy.com", options =>
{
    options.Enabled = false;
});
```

## Benefits and Impact

### Performance Improvements
- **Reduced latency**: Cached responses eliminate network round-trips
- **Lower server load**: Fewer requests to the API server
- **Better user experience**: Faster application responses

### Cost Optimization
- **Reduced bandwidth**: Less data transfer
- **Lower API costs**: Fewer billable API calls
- **Resource efficiency**: Better resource utilization

### Reliability Enhancement
- **Offline resilience**: Cached data available during network issues
- **Reduced dependencies**: Less reliance on API availability
- **Graceful degradation**: Stale data better than no data

## Risk Mitigation

### Data Consistency
- **Cache invalidation**: Automatic invalidation on mutations
- **ETag support**: Conditional requests for validation
- **Configurable expiry**: Appropriate cache timeouts

### Memory Management
- **Size limits**: Configurable maximum cache entry size
- **Automatic eviction**: Memory pressure handling
- **Monitoring**: Cache hit/miss metrics

### Flexibility
- **Configurable**: Enable/disable caching per environment
- **Storage options**: Memory or distributed cache
- **Fine-grained control**: Per-operation cache settings

This comprehensive plan provides a robust, flexible, and maintainable client-side caching solution for the Marain.Tenancy.Client that respects HTTP caching standards while optimizing performance and reliability.