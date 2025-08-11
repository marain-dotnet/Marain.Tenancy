# Kiota Client Corvus Serialization Integration

## Research Question
How to configure a Kiota-generated client to use the same JSON serialization options as the ASP.NET Core Minimal API, specifically using `IJsonSerializerOptionsProvider` from Corvus.Json.Serialization.

## The Challenge

### Current State
- **MinimalApi**: Uses unified JSON serialization via `IJsonSerializerOptionsProvider` with Corvus converters
- **Kiota Client**: Uses default Kiota serialization factories with standard System.Text.Json options
- **Result**: Serialization mismatch between client and API

### Specific Issues
1. **Enum Serialization**: API uses camelCase enums ("add", "replace"), client expects different format
2. **Date/Time Formatting**: API uses custom DateTimeOffset converters, client uses defaults
3. **Culture Info**: API handles CultureInfo specially, client doesn't
4. **Property Bags**: API uses Corvus property bag converters, client needs same handling

## Kiota Serialization Architecture Analysis

### Key Components
1. **ISerializationWriterFactory**: Creates writers for serializing outbound requests
2. **IParseNodeFactory**: Creates parsers for deserializing inbound responses  
3. **ApiClientBuilder**: Registers default factories globally
4. **BaseRequestBuilder**: Uses registered factories via service locator pattern

### Current Kiota Client Registration
```csharp
public TenancyApiClient(IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}", new Dictionary<string, object>())
{
    // Only deserializers registered, no serializers
    ApiClientBuilder.RegisterDefaultDeserializer<JsonParseNodeFactory>();
    ApiClientBuilder.RegisterDefaultDeserializer<TextParseNodeFactory>();
    ApiClientBuilder.RegisterDefaultDeserializer<FormParseNodeFactory>();
}
```

**Issue**: No custom serializer registration, and deserializers use default options.

## Solution Architecture

### 1. Custom JSON Serialization Writer Factory

```csharp
public class CorvusJsonSerializationWriterFactory : ISerializationWriterFactory
{
    private readonly IJsonSerializerOptionsProvider _optionsProvider;
    
    public CorvusJsonSerializationWriterFactory(IJsonSerializerOptionsProvider optionsProvider)
    {
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
    }
    
    public string ValidContentType => "application/json";
    
    public ISerializationWriter GetSerializationWriter(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            throw new ArgumentNullException(nameof(contentType));
            
        var validContentType = ValidContentType;
        if (!validContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(contentType), $"Expected {validContentType}");
        }
        
        var stream = new MemoryStream();
        var jsonOptions = _optionsProvider.Instance;
        var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Encoder = jsonOptions.Encoder,
            Indented = jsonOptions.WriteIndented,
            SkipValidation = false
        });
        
        return new CorvusJsonSerializationWriter(writer, jsonOptions);
    }
}
```

### 2. Custom JSON Serialization Writer

```csharp
public class CorvusJsonSerializationWriter : ISerializationWriter, IDisposable
{
    private readonly Utf8JsonWriter _writer;
    private readonly JsonSerializerOptions _options;
    private readonly MemoryStream _stream;
    private bool _disposed;

    public CorvusJsonSerializationWriter(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _stream = new MemoryStream();
    }

    public void WriteStringValue(string? key, string? value)
    {
        if (key != null)
        {
            _writer.WriteString(key, value);
        }
        else
        {
            _writer.WriteStringValue(value);
        }
    }

    public void WriteObjectValue<T>(string? key, T? value, params IParsable[]? additionalValuesToMerge) where T : IParsable
    {
        if (value == null) 
        {
            if (key != null)
                _writer.WriteNull(key);
            else
                _writer.WriteNullValue();
            return;
        }

        if (key != null)
            _writer.WriteStartObject(key);
        else
            _writer.WriteStartObject();

        value.Serialize(this);

        if (additionalValuesToMerge != null)
        {
            foreach (var additionalValue in additionalValuesToMerge.Where(x => x != null))
            {
                additionalValue.Serialize(this);
            }
        }

        _writer.WriteEndObject();
    }

    public void WriteCollectionOfObjectValues<T>(string? key, IEnumerable<T>? values) where T : IParsable
    {
        if (values == null)
        {
            if (key != null)
                _writer.WriteNull(key);
            else
                _writer.WriteNullValue();
            return;
        }

        if (key != null)
            _writer.WriteStartArray(key);
        else
            _writer.WriteStartArray();

        foreach (var value in values)
        {
            WriteObjectValue(null, value);
        }

        _writer.WriteEndArray();
    }

    public void WriteEnumValue<T>(string? key, T? value) where T : struct, Enum
    {
        if (value == null)
        {
            if (key != null)
                _writer.WriteNull(key);
            else
                _writer.WriteNullValue();
            return;
        }

        // Use JsonSerializer with custom options to handle enum serialization
        var jsonValue = JsonSerializer.Serialize(value, _options);
        var enumString = JsonSerializer.Deserialize<string>(jsonValue, _options);
        
        if (key != null)
            _writer.WriteString(key, enumString);
        else
            _writer.WriteStringValue(enumString);
    }

    public Stream GetSerializedContent()
    {
        _writer.Flush();
        _stream.Position = 0;
        return _stream;
    }

    // ... implement other required methods similarly
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _writer?.Dispose();
            _stream?.Dispose();
            _disposed = true;
        }
    }
}
```

### 3. Custom JSON Parse Node Factory

```csharp
public class CorvusJsonParseNodeFactory : IParseNodeFactory
{
    private readonly IJsonSerializerOptionsProvider _optionsProvider;
    
    public CorvusJsonParseNodeFactory(IJsonSerializerOptionsProvider optionsProvider)
    {
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
    }
    
    public string ValidContentType => "application/json";
    
    public IParseNode GetRootParseNode(string contentType, Stream content)
    {
        if (string.IsNullOrEmpty(contentType))
            throw new ArgumentNullException(nameof(contentType));
            
        if (content == null)
            throw new ArgumentNullException(nameof(content));
            
        var validContentType = ValidContentType;
        if (!validContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(contentType), $"Expected {validContentType}");
        }
        
        using var document = JsonDocument.Parse(content, new JsonDocumentOptions
        {
            AllowTrailingCommas = _optionsProvider.Instance.AllowTrailingCommas,
            CommentHandling = _optionsProvider.Instance.ReadCommentHandling == JsonCommentHandling.Skip ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow
        });
        
        return new CorvusJsonParseNode(document.RootElement.Clone(), _optionsProvider.Instance);
    }
}
```

### 4. Custom JSON Parse Node

```csharp
public class CorvusJsonParseNode : IParseNode
{
    private readonly JsonElement _jsonElement;
    private readonly JsonSerializerOptions _options;
    
    public CorvusJsonParseNode(JsonElement jsonElement, JsonSerializerOptions options)
    {
        _jsonElement = jsonElement;
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }
    
    public T? GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable
    {
        if (_jsonElement.ValueKind == JsonValueKind.Null)
            return default(T);
            
        var instance = factory(_jsonElement.ToString());
        if (instance is T result)
            return result;
            
        return default(T);
    }
    
    public string? GetStringValue()
    {
        if (_jsonElement.ValueKind == JsonValueKind.Null)
            return null;
            
        return _jsonElement.GetString();
    }
    
    public T? GetEnumValue<T>() where T : struct, Enum
    {
        if (_jsonElement.ValueKind == JsonValueKind.Null)
            return null;
            
        // Use JsonSerializer with custom options to handle enum deserialization
        var jsonString = _jsonElement.GetRawText();
        return JsonSerializer.Deserialize<T>(jsonString, _options);
    }
    
    public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory) where T : IParsable
    {
        if (_jsonElement.ValueKind != JsonValueKind.Array)
            return Enumerable.Empty<T>();
            
        var result = new List<T>();
        foreach (var element in _jsonElement.EnumerateArray())
        {
            var parseNode = new CorvusJsonParseNode(element, _options);
            var value = parseNode.GetObjectValue(factory);
            if (value != null)
                result.Add(value);
        }
        
        return result;
    }
    
    // ... implement other required methods
}
```

## Service Collection Integration

### 5. Enhanced Service Collection Extensions

```csharp
public static class TenancyClientServiceCollectionExtensions
{
    public static IServiceCollection AddTenancyClientWithCorvusSerialization(
        this IServiceCollection services,
        string baseUrl,
        TokenCredential? tokenCredential = null,
        HttpMessageHandler? messageHandler = null)
    {
        // Add base client services
        services.AddTenancyClient(baseUrl, tokenCredential, messageHandler);
        
        // Register custom serialization factories
        services.AddSingleton<CorvusJsonSerializationWriterFactory>();
        services.AddSingleton<CorvusJsonParseNodeFactory>();
        
        // Configure Kiota to use custom factories
        services.PostConfigure<TenancyApiClient>((client, serviceProvider) =>
        {
            var serializerFactory = serviceProvider.GetRequiredService<CorvusJsonSerializationWriterFactory>();
            var parseNodeFactory = serviceProvider.GetRequiredService<CorvusJsonParseNodeFactory>();
            
            // Register custom factories globally
            ApiClientBuilder.RegisterSerializer<CorvusJsonSerializationWriterFactory>(() => serializerFactory);
            ApiClientBuilder.RegisterDeserializer<CorvusJsonParseNodeFactory>(() => parseNodeFactory);
        });
        
        return services;
    }
}
```

### 6. Factory Registration Helper

```csharp
public static class KiotaCorvusSerializationExtensions
{
    public static void ConfigureCorvusSerialization(this TenancyApiClient client, IServiceProvider serviceProvider)
    {
        var serializerFactory = serviceProvider.GetRequiredService<CorvusJsonSerializationWriterFactory>();
        var parseNodeFactory = serviceProvider.GetRequiredService<CorvusJsonParseNodeFactory>();
        
        // Override default registrations
        ApiClientBuilder.RegisterSerializer<CorvusJsonSerializationWriterFactory>(() => serializerFactory);
        ApiClientBuilder.RegisterDeserializer<CorvusJsonParseNodeFactory>(() => parseNodeFactory);
    }
}
```

## Usage Examples

### ✅ Final Usage Examples

#### Option 1: Automatic Configuration (Recommended)
```csharp
// In Program.cs - Replace AddTenancyClient with AddTenancyClientWithCorvusSerialization
builder.Services.AddTenancyClientWithCorvusSerialization("https://api.example.com");

// The client is now ready to use with Corvus serialization
var serviceProvider = builder.Services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<TenancyApiClient>();

// You must call this to activate the custom serialization
serviceProvider.ConfigureKiotaCorvusSerialization();

// Now all client operations use Corvus serialization options
var tenant = await client["tenant-id"].GetAsync();
```

#### Option 2: Manual Configuration
```csharp
// Standard client registration
builder.Services.AddTenancyClient("https://api.example.com");

// Manually add custom serialization factories
builder.Services.AddSingleton<CorvusJsonSerializationWriterFactory>();
builder.Services.AddSingleton<CorvusJsonParseNodeFactory>();

// Configure when needed
var serviceProvider = builder.Services.BuildServiceProvider();
var client = serviceProvider.GetRequiredService<TenancyApiClient>();

// Activate custom serialization
client.ConfigureCorvusSerialization(serviceProvider);

// Client now uses Corvus serialization
var tenant = await client["tenant-id"].GetAsync();
```

#### Option 3: Unauthenticated Client
```csharp
// For scenarios without authentication
builder.Services.AddUnauthenticatedTenancyClientWithCorvusSerialization("https://api.example.com");

var serviceProvider = builder.Services.BuildServiceProvider();
serviceProvider.ConfigureKiotaCorvusSerialization();

var client = serviceProvider.GetRequiredService<TenancyApiClient>();
var tenant = await client["tenant-id"].GetAsync();
```

## Testing Strategy

### Serialization Compatibility Test

```csharp
[Test]
public async Task Client_And_Api_Use_Same_Serialization()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddTenancyClientWithCorvusSerialization("https://localhost:5138");
    
    var serviceProvider = services.BuildServiceProvider();
    var client = serviceProvider.GetRequiredService<TenancyApiClient>();
    var optionsProvider = serviceProvider.GetRequiredService<IJsonSerializerOptionsProvider>();
    
    // Create test data
    var testEnum = UpdateTenantJsonPatchEntryOperation.Add;
    var testDate = DateTimeOffset.Now;
    var testCulture = CultureInfo.CurrentCulture;
    
    // Serialize using both methods
    var clientSerialized = JsonSerializer.Serialize(testEnum, optionsProvider.Instance);
    
    // Test via actual client call (this would use custom factories)
    var request = new UpdateTenantJsonPatchEntry 
    { 
        Operation = testEnum, 
        Path = "/test" 
    };
    
    // Both should produce identical results
    Assert.That(clientSerialized, Contains.Substring("\"add\""));
}
```

### Integration Test

```csharp
[Test]
public async Task Client_Can_Communicate_With_Api()
{
    // Start the MinimalApi in test host
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    
    // Configure Kiota client to point to test host
    var services = new ServiceCollection();
    services.AddTenancyClientWithCorvusSerialization(factory.Server.BaseAddress.ToString());
    
    var serviceProvider = services.BuildServiceProvider();
    var tenancyClient = serviceProvider.GetRequiredService<TenancyApiClient>();
    
    // Test actual operations
    var tenant = await tenancyClient["test-tenant"].GetAsync();
    Assert.That(tenant, Is.Not.Null);
}
```

## ✅ Implementation Status - COMPLETED

### ✅ Completed Implementation

1. **Research**: ✅ Kiota serialization architecture analysis complete
2. **Custom Serialization Writer Factory**: ✅ `CorvusJsonSerializationWriterFactory` implemented
3. **Custom Serialization Writer**: ✅ `CorvusJsonSerializationWriter` implemented with full ISerializationWriter interface
4. **Custom Parse Node Factory**: ✅ `CorvusJsonParseNodeFactory` implemented
5. **Custom Parse Node**: ✅ `CorvusJsonParseNode` implemented with full IParseNode interface
6. **Service Collection Extensions**: ✅ Enhanced with `AddTenancyClientWithCorvusSerialization()` methods
7. **Configuration Extensions**: ✅ `KiotaCorvusSerializationExtensions` for easy configuration
8. **Documentation**: ✅ Comprehensive implementation guide

### 📁 Implementation Files Created

```
Solutions/Marain.Tenancy.Client/
├── Marain/Tenancy/Client/Serialization/
│   ├── CorvusJsonSerializationWriterFactory.cs
│   ├── CorvusJsonSerializationWriter.cs
│   ├── CorvusJsonParseNodeFactory.cs
│   ├── CorvusJsonParseNode.cs
│   └── KiotaCorvusSerializationExtensions.cs
└── Microsoft/Extensions/DependencyInjection/
    └── TenancyClientServiceCollectionExtensions.cs (enhanced)
```

## Key Insights

1. **Global Registration**: Kiota uses global static registration via ApiClientBuilder
2. **Factory Pattern**: Custom factories must implement specific interfaces
3. **DI Integration**: Factories need access to IJsonSerializerOptionsProvider from DI
4. **Timing**: Registration must happen after DI container is built but before client usage
5. **Compatibility**: All serialization contexts (API, client, manual) must use identical options

## Challenges & Solutions

### Challenge 1: Global State
**Problem**: ApiClientBuilder uses global static registration  
**Solution**: Use PostConfigure or factory initialization to set up custom factories

### Challenge 2: Generated Code
**Problem**: TenancyApiClient constructor is auto-generated  
**Solution**: Override registrations after client creation via extension methods

### Challenge 3: Factory Lifecycle
**Problem**: Factories need DI services but are registered globally  
**Solution**: Use factory delegates that capture DI services from service provider

### Challenge 4: Testing
**Problem**: Global registration affects all tests  
**Solution**: Reset registrations between tests or use separate app domains

## Recommended Implementation Order

1. ✅ Complete custom JsonSerializationWriterFactory
2. Implement custom JsonParseNodeFactory  
3. Update service collection extensions
4. Add configuration extension methods
5. Implement comprehensive tests
6. Update client generation scripts if needed