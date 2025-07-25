# Migration Plan: Menes Framework to ASP.NET Core Minimal APIs

## Executive Summary

This document outlines a comprehensive migration strategy to move the Marain.Tenancy solution from the Menes framework to native ASP.NET Core Minimal APIs. The migration aims to simplify the codebase, improve performance, reduce dependencies, and align with modern .NET development practices.

## Current Architecture Analysis

### Menes Framework Components Identified

1. **TenancyService**: Implements `IOpenApiService` with operation-specific methods
   - Operations: `getTenant`, `updateTenant`, `getChildren`, `createChildTenant`, `deleteChildTenant`
   - Uses `[OperationId]` attributes for method mapping
   - Embedded OpenAPI specification via `[EmbeddedOpenApiDefinition]`

2. **HAL (Hypertext Application Language) Support**:
   - `TenantMapper` implements `IHalDocumentMapper<ITenant>`
   - `TenantCollectionResultMapper` for collection responses
   - Link resolution through `IOpenApiWebLinkResolver`
   - Self and related links embedded in responses

3. **Hosting Abstraction**:
   - **ASP.NET Core**: `AddTenancyApiWithAspNetPipelineHosting()`
   - **Azure Functions**: `IOpenApiHost<HttpRequest, IActionResult>`
   - Common service registration through `AddEverythingExceptHosting()`

4. **Testing Infrastructure**:
   - Multi-host testing with `ITestableTenancyService`
   - Direct service invocation via `DirectTestableTenancyService`
   - HTTP client testing via `ClientTestableTenancyService`

### Key Dependencies to Remove

- `Menes.Abstractions` (v6.0.3)
- HAL document generation and link resolution
- OpenAPI service hosting abstraction
- Menes-specific testing utilities

## Migration Strategy

### Phase 1: Foundation Preparation (Low Risk)

#### 1.1: Create New Minimal API Infrastructure

**Timeline**: 1-2 weeks

**Deliverables**:
- New project structure for Minimal APIs
- Base service registration extensions
- OpenAPI configuration setup
- Logging and exception handling middleware

**Implementation**:

```csharp
// New file: Extensions/MinimalApiExtensions.cs
public static class MinimalApiExtensions
{
    public static WebApplicationBuilder AddTenancyMinimalApi(this WebApplicationBuilder builder)
    {
        // Service registrations
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Core tenancy services (existing)
        builder.Services.AddTenancyCore();
        
        // Validation
        builder.Services.AddValidatorsFromAssemblyContaining<TenantRequestValidator>();
        
        return builder;
    }
    
    public static WebApplication MapTenancyEndpoints(this WebApplication app)
    {
        var tenants = app.MapGroup("/api/tenants")
            .WithTags("Tenancy")
            .WithOpenApi();
            
        tenants.RegisterTenantEndpoints();
        
        return app;
    }
}
```

#### 1.2: Define Request/Response Models

**Replace HAL documents with dedicated DTOs**:

```csharp
// Models/Requests/UpdateTenantRequest.cs
public record UpdateTenantRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    
    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; init; }
}

// Models/Responses/TenantResponse.cs
public record TenantResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }
    
    [JsonPropertyName("properties")]
    public IPropertyBag? Properties { get; init; }
    
    [JsonPropertyName("_links")]
    public Dictionary<string, LinkResponse>? Links { get; init; }
}

// Models/Responses/LinkResponse.cs
public record LinkResponse
{
    [JsonPropertyName("href")]
    public required string Href { get; init; }
    
    [JsonPropertyName("templated")]
    public bool Templated { get; init; } = false;
}
```

### Phase 2: Core Endpoint Implementation (Medium Risk)

#### 2.1: Implement Minimal API Endpoints

**Timeline**: 2-3 weeks

**Approach**: Feature-based organization with extension methods

```csharp
// Endpoints/TenantEndpoints.cs
public static class TenantEndpoints
{
    public static void RegisterTenantEndpoints(this IEndpointRouteBuilder routes)
    {
        var tenants = routes.MapGroup("/{tenantId}/marain/tenant")
            .WithTags("Tenancy")
            .AddValidation<TenantRequestValidator>();

        tenants.MapGet("", GetTenantAsync)
            .WithName("GetTenant")
            .WithSummary("Gets a tenant")
            .Produces<TenantResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status304NotModified);

        tenants.MapPatch("", UpdateTenantAsync)
            .WithName("UpdateTenant")
            .WithSummary("Updates a tenant")
            .Accepts<JsonPatchDocument>("application/json-patch+json")
            .Produces<TenantResponse>()
            .ProducesValidationProblem();

        // Additional endpoints...
    }

    private static async Task<IResult> GetTenantAsync(
        string tenantId,
        [FromHeader(Name = "If-None-Match")] string? etag,
        ITenantStore tenantStore,
        ITenantMapper tenantMapper,
        TenantCacheConfiguration cacheConfig,
        ILogger<Program> logger)
    {
        try
        {
            ITenant tenant = tenantId == RootTenant.RootTenantId
                ? GetRedactedRootTenant()
                : await tenantStore.GetTenantAsync(tenantId, etag);

            var response = await tenantMapper.MapToResponseAsync(tenant);
            
            var result = Results.Ok(response);
            
            if (!string.IsNullOrEmpty(tenant.ETag))
            {
                // Add ETag header
                HttpContext.Current.Response.Headers.ETag = tenant.ETag;
            }
            
            if (!string.IsNullOrEmpty(cacheConfig.GetTenantResponseCacheControlHeaderValue))
            {
                HttpContext.Current.Response.Headers.CacheControl = 
                    cacheConfig.GetTenantResponseCacheControlHeaderValue;
            }
            
            return result;
        }
        catch (TenantNotModifiedException)
        {
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }
        catch (TenantNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> UpdateTenantAsync(
        string tenantId,
        JsonPatchDocument patchDocument,
        ITenantStore tenantStore,
        ITenantMapper tenantMapper,
        ILogger<Program> logger)
    {
        if (tenantId == RootTenant.RootTenantId)
        {
            return Results.StatusCode(StatusCodes.Status405MethodNotAllowed);
        }

        try
        {
            // Extract operations from patch document
            var (name, propertiesToSet, propertiesToRemove) = 
                ExtractPatchOperations(patchDocument);

            var updatedTenant = await tenantStore.UpdateTenantAsync(
                tenantId, name, propertiesToSet, propertiesToRemove);

            var response = await tenantMapper.MapToResponseAsync(updatedTenant);
            return Results.Ok(response);
        }
        catch (TenantNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException)
        {
            return Results.Forbid();
        }
    }
}
```

#### 2.2: Replace Mappers

**Remove HAL dependencies and create standard mappers**:

```csharp
// Mappers/ITenantMapper.cs
public interface ITenantMapper
{
    Task<TenantResponse> MapToResponseAsync(ITenant tenant);
    Task<ChildTenantsResponse> MapToChildrenResponseAsync(TenantCollectionResult result);
}

// Mappers/TenantMapper.cs
public class TenantMapper : ITenantMapper
{
    private readonly ILinkGenerator linkGenerator;
    
    public TenantMapper(ILinkGenerator linkGenerator)
    {
        this.linkGenerator = linkGenerator;
    }
    
    public Task<TenantResponse> MapToResponseAsync(ITenant tenant)
    {
        var response = new TenantResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            ContentType = tenant.ContentType,
            Properties = tenant.Properties,
            Links = new Dictionary<string, LinkResponse>
            {
                ["self"] = new LinkResponse 
                { 
                    Href = linkGenerator.GetUriByName("GetTenant", new { tenantId = tenant.Id })
                },
                ["children"] = new LinkResponse 
                { 
                    Href = linkGenerator.GetUriByName("GetChildren", new { tenantId = tenant.Id })
                }
            }
        };
        
        return Task.FromResult(response);
    }
}
```

### Phase 3: Hosting Migration (Medium Risk)

#### 3.1: ASP.NET Core Host Migration

**Timeline**: 1 week

**Replace Menes hosting with native ASP.NET Core**:

```csharp
// New Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.AddTenancyMinimalApi();

// Add storage
var rootStorageConfig = builder.Configuration
    .GetSection("RootBlobStorageConfiguration")
    .Get<BlobContainerConfiguration>();
builder.Services.AddTenantStoreOnAzureBlobStorage(rootStorageConfig);

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapTenancyEndpoints();

app.Run();
```

#### 3.2: Azure Functions Host Migration

**Timeline**: 1-2 weeks

**Replace Menes Functions hosting**:

```csharp
// Functions/TenancyFunctions.cs
public class TenancyFunctions
{
    private readonly WebApplication app;
    
    public TenancyFunctions()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddTenancyMinimalApi();
        
        // Configure for Functions
        builder.Services.Configure<RouteOptions>(options =>
        {
            options.LowercaseUrls = false; // Functions are case-sensitive
        });
        
        app = builder.Build();
        app.MapTenancyEndpoints();
    }
    
    [FunctionName("GetTenant")]
    public async Task<IActionResult> GetTenant(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", 
         Route = "{tenantId}/marain/tenant")] HttpRequest req,
        string tenantId)
    {
        // Delegate to Minimal API app
        return await app.HandleAsync(req);
    }
    
    // Additional function endpoints...
}
```

### Phase 4: Testing Migration (High Risk)

#### 4.1: Integration Testing with WebApplicationFactory

**Timeline**: 2-3 weeks

**Replace multi-host testing with WebApplicationFactory**:

```csharp
// Tests/Integration/TenancyApiTests.cs
public class TenancyApiTests : IClassFixture<TenancyWebApplicationFactory>
{
    private readonly TenancyWebApplicationFactory factory;
    private readonly HttpClient client;

    public TenancyApiTests(TenancyWebApplicationFactory factory)
    {
        this.factory = factory;
        this.client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTenant_ExistingTenant_ReturnsOk()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        
        // Act
        var response = await client.GetAsync($"/{tenantId}/marain/tenant");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var tenant = JsonSerializer.Deserialize<TenantResponse>(content);
        
        tenant.Should().NotBeNull();
        tenant!.Id.Should().Be(tenantId);
    }

    [Fact]
    public async Task UpdateTenant_ValidPatch_ReturnsUpdatedTenant()
    {
        // Arrange
        var tenantId = "test-tenant-id";
        var patchDoc = new JsonPatchDocument();
        patchDoc.Replace("/name", "Updated Name");
        
        var json = JsonSerializer.Serialize(patchDoc);
        var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");
        
        // Act
        var response = await client.PatchAsync($"/{tenantId}/marain/tenant", content);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

// Tests/TenancyWebApplicationFactory.cs
public class TenancyWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace storage with in-memory version
            services.RemoveAll<ITenantStore>();
            services.AddSingleton<ITenantStore, InMemoryTenantStore>();
            
            // Configure test database
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
        });
        
        builder.UseEnvironment("Testing");
    }
}
```

#### 4.2: Unit Testing Individual Endpoints

```csharp
// Tests/Unit/TenantEndpointsTests.cs
public class TenantEndpointsTests
{
    [Fact]
    public async Task GetTenantAsync_ExistingTenant_ReturnsCorrectResponse()
    {
        // Arrange
        var tenantStore = new Mock<ITenantStore>();
        var tenantMapper = new Mock<ITenantMapper>();
        var logger = new Mock<ILogger<Program>>();
        var cacheConfig = new TenantCacheConfiguration();
        
        var tenant = new Mock<ITenant>();
        tenant.Setup(t => t.Id).Returns("test-id");
        tenant.Setup(t => t.Name).Returns("Test Tenant");
        
        tenantStore.Setup(s => s.GetTenantAsync("test-id", null))
                  .ReturnsAsync(tenant.Object);
                  
        var expectedResponse = new TenantResponse
        {
            Id = "test-id",
            Name = "Test Tenant",
            ContentType = "application/vnd.marain.tenancy.tenant"
        };
        
        tenantMapper.Setup(m => m.MapToResponseAsync(tenant.Object))
                   .ReturnsAsync(expectedResponse);
        
        // Act
        var result = await TenantEndpoints.GetTenantAsync(
            "test-id", null, tenantStore.Object, tenantMapper.Object, 
            cacheConfig, logger.Object);
        
        // Assert
        result.Should().BeOfType<Ok<TenantResponse>>();
        var okResult = (Ok<TenantResponse>)result;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }
}
```

### Phase 5: Validation and Error Handling (Medium Risk)

#### 5.1: OpenAPI-Driven Request Validation

**Analyze TenancyServices.yaml validation requirements and implement comprehensive validation**:

The existing OpenAPI specification defines detailed validation rules that must be preserved during migration:

##### Parameter Validation Requirements

Based on the OpenAPI specification parameters section:

```yaml
# From TenancyServices.yaml - Parameter definitions to implement
parameters:
  tenantId:           # Required path parameter - string
  tenantName:         # Required query parameter - string  
  wellKnownChildTenantGuid: # Optional query parameter - UUID format
  childTenantId:      # Required path parameter - string
  continuationToken:  # Optional query parameter - string
  maxItems:          # Optional query parameter - integer
  ifNoneMatch:       # Optional header parameter - string (ETag)
```

##### JSON Patch Validation Requirements

The `UpdateTenantJsonPatchEntry` schema defines strict validation rules:

```yaml
# From TenancyServices.yaml - JSON Patch validation rules
UpdateTenantJsonPatchEntry:
  required: ["op", "path"]
  properties:
    path: 
      type: string 
      description: "A JSON-Pointer. Either /name or /properties/propertyName"
    op: 
      type: string 
      enum: [add, replace, remove]  # Only these operations allowed
    value: 
      anyOf: [string, object, array, boolean, integer, number]
```

##### Implementation Strategy

**1. Parameter Validation using FluentValidation**:

```csharp
// Validation/Parameters/TenantParametersValidator.cs
public class TenantParametersValidator : AbstractValidator<TenantParameters>
{
    public TenantParametersValidator()
    {
        // tenantId - Required path parameter
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        // tenantName - Required for child tenant creation
        RuleFor(x => x.TenantName)
            .NotEmpty()
            .When(x => x.IsCreateChildTenantRequest)
            .WithMessage("TenantName is required for child tenant creation");

        // wellKnownChildTenantGuid - Optional UUID validation
        RuleFor(x => x.WellKnownChildTenantGuid)
            .Must(BeValidGuid)
            .When(x => !string.IsNullOrEmpty(x.WellKnownChildTenantGuid))
            .WithMessage("WellKnownChildTenantGuid must be a valid UUID");

        // maxItems - Optional integer with reasonable bounds
        RuleFor(x => x.MaxItems)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .When(x => x.MaxItems.HasValue)
            .WithMessage("MaxItems must be between 1 and 1000");

        // childTenantId - Required for delete operations
        RuleFor(x => x.ChildTenantId)
            .NotEmpty()
            .When(x => x.IsDeleteChildTenantRequest)
            .WithMessage("ChildTenantId is required for delete operations");
    }

    private static bool BeValidGuid(string? guid)
    {
        return Guid.TryParse(guid, out _);
    }
}

// Models/TenantParameters.cs
public record TenantParameters
{
    public string TenantId { get; init; } = string.Empty;
    public string? TenantName { get; init; }
    public string? WellKnownChildTenantGuid { get; init; }
    public string? ChildTenantId { get; init; }
    public string? ContinuationToken { get; init; }
    public int? MaxItems { get; init; }
    public string? IfNoneMatch { get; init; }
    
    // Context flags for conditional validation
    public bool IsCreateChildTenantRequest { get; init; }
    public bool IsDeleteChildTenantRequest { get; init; }
}
```

**2. JSON Patch Validation based on OpenAPI Schema**:

```csharp
// Validation/JsonPatch/JsonPatchValidator.cs
public class JsonPatchValidator : AbstractValidator<JsonPatchDocument>
{
    private static readonly string[] AllowedOperations = { "add", "replace", "remove" };
    private static readonly string[] AllowedPaths = { "/name" };
    private const string PropertyPathPrefix = "/properties/";

    public JsonPatchValidator()
    {
        RuleFor(x => x.Operations)
            .NotEmpty()
            .WithMessage("At least one operation is required");

        RuleForEach(x => x.Operations)
            .SetValidator(new JsonPatchOperationValidator());
    }
}

public class JsonPatchOperationValidator : AbstractValidator<Operation>
{
    public JsonPatchOperationValidator()
    {
        // Validate 'op' field - must be one of allowed operations
        RuleFor(x => x.OperationType)
            .Must(BeAllowedOperation)
            .WithMessage("Operation must be one of: add, replace, remove");

        // Validate 'path' field - must match allowed patterns
        RuleFor(x => x.path)
            .NotEmpty()
            .Must(BeValidPath)
            .WithMessage("Path must be '/name' or '/properties/{propertyName}'");

        // Validate 'value' field - required for add/replace operations
        RuleFor(x => x.value)
            .NotNull()
            .When(x => x.OperationType == OperationType.Add || x.OperationType == OperationType.Replace)
            .WithMessage("Value is required for add and replace operations");

        // Validate 'value' field - not allowed for remove operations
        RuleFor(x => x.value)
            .Null()
            .When(x => x.OperationType == OperationType.Remove)
            .WithMessage("Value should not be provided for remove operations");

        // Validate property name format for property operations
        RuleFor(x => x.path)
            .Must(HaveValidPropertyName)
            .When(x => x.path?.StartsWith("/properties/") == true)
            .WithMessage("Property name must not be empty and contain valid characters");
    }

    private static bool BeAllowedOperation(OperationType operationType)
    {
        return operationType == OperationType.Add || 
               operationType == OperationType.Replace || 
               operationType == OperationType.Remove;
    }

    private static bool BeValidPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        // Allow /name path
        if (path == "/name")
            return true;

        // Allow /properties/{propertyName} paths
        if (path.StartsWith("/properties/") && path.Length > "/properties/".Length)
            return true;

        return false;
    }

    private static bool HaveValidPropertyName(string? path)
    {
        if (path?.StartsWith("/properties/") != true)
            return true; // Not a property path, so this rule doesn't apply

        var propertyName = path["/properties/".Length..];
        return !string.IsNullOrWhiteSpace(propertyName) && 
               propertyName.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-');
    }
}
```

**3. Content-Type Validation**:

```csharp
// Validation/ContentType/ContentTypeValidator.cs
public class ContentTypeValidationFilter : IEndpointFilter
{
    private static readonly Dictionary<string, string[]> EndpointContentTypes = new()
    {
        ["PATCH"] = new[] { "application/json-patch+json" },
        ["POST"] = new[] { "application/json" },
        ["PUT"] = new[] { "application/json" }
    };

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, 
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var method = httpContext.Request.Method;

        if (EndpointContentTypes.TryGetValue(method, out var allowedTypes))
        {
            var contentType = httpContext.Request.ContentType;
            if (string.IsNullOrEmpty(contentType) || 
                !allowedTypes.Any(allowed => contentType.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)))
            {
                return Results.BadRequest($"Content-Type must be one of: {string.Join(", ", allowedTypes)}");
            }
        }

        return await next(context);
    }
}
```

**4. Endpoint-Specific Validation Integration**:

```csharp
// Endpoints/TenantEndpoints.cs - Updated with OpenAPI-compliant validation
public static class TenantEndpoints
{
    public static void RegisterTenantEndpoints(this IEndpointRouteBuilder routes)
    {
        var tenants = routes.MapGroup("/{tenantId}/marain/tenant")
            .WithTags("Tenancy")
            .AddEndpointFilter<ContentTypeValidationFilter>();

        // GET /{tenantId}/marain/tenant
        tenants.MapGet("", GetTenantAsync)
            .WithName("GetTenant")
            .AddEndpointFilter<ParameterValidationFilter<GetTenantParameters>>();

        // PATCH /{tenantId}/marain/tenant
        tenants.MapPatch("", UpdateTenantAsync)
            .WithName("UpdateTenant")
            .AddEndpointFilter<ParameterValidationFilter<UpdateTenantParameters>>()
            .AddEndpointFilter<ValidationFilter<JsonPatchDocument>>();

        // Child tenant endpoints
        var children = tenants.MapGroup("/children");

        // GET /{tenantId}/marain/tenant/children
        children.MapGet("", GetChildTenantsAsync)
            .WithName("GetChildren")
            .AddEndpointFilter<ParameterValidationFilter<GetChildrenParameters>>();

        // POST /{tenantId}/marain/tenant/children
        children.MapPost("", CreateChildTenantAsync)
            .WithName("CreateChildTenant")
            .AddEndpointFilter<ParameterValidationFilter<CreateChildTenantParameters>>();

        // DELETE /{tenantId}/marain/tenant/children/{childTenantId}
        children.MapDelete("/{childTenantId}", DeleteChildTenantAsync)
            .WithName("DeleteChildTenant")
            .AddEndpointFilter<ParameterValidationFilter<DeleteChildTenantParameters>>();
    }

    private static async Task<IResult> UpdateTenantAsync(
        [AsParameters] UpdateTenantParameters parameters,
        JsonPatchDocument patchDocument,
        ITenantStore tenantStore,
        ITenantMapper tenantMapper,
        ILogger<Program> logger)
    {
        // Validation is handled by endpoint filters
        // Business logic validation
        if (parameters.TenantId == RootTenant.RootTenantId)
        {
            return Results.StatusCode(StatusCodes.Status405MethodNotAllowed);
        }

        try
        {
            var (name, propertiesToSet, propertiesToRemove) = 
                ExtractPatchOperations(patchDocument);

            var updatedTenant = await tenantStore.UpdateTenantAsync(
                parameters.TenantId, name, propertiesToSet, propertiesToRemove);

            var response = await tenantMapper.MapToResponseAsync(updatedTenant);
            return Results.Ok(response);
        }
        catch (TenantNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
    }
}

// Parameters models matching OpenAPI specification
public record GetTenantParameters
{
    public required string TenantId { get; init; }
    [FromHeader(Name = "If-None-Match")]
    public string? IfNoneMatch { get; init; }
}

public record UpdateTenantParameters
{
    public required string TenantId { get; init; }
}

public record GetChildrenParameters
{
    public required string TenantId { get; init; }
    [FromQuery]
    public string? ContinuationToken { get; init; }
    [FromQuery]
    public int? MaxItems { get; init; }
}

public record CreateChildTenantParameters
{
    public required string TenantId { get; init; }
    [FromQuery]
    public required string TenantName { get; init; }
    [FromQuery]
    public string? WellKnownChildTenantGuid { get; init; }
}

public record DeleteChildTenantParameters
{
    public required string TenantId { get; init; }
    public required string ChildTenantId { get; init; }
}
```

**5. Validation Error Response Format**:

```csharp
// Responses/ValidationProblemDetailsExtensions.cs
public static class ValidationProblemDetailsExtensions
{
    public static IResult CreateValidationProblem(this ValidationResult validationResult)
    {
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return Results.ValidationProblem(
            errors: errors,
            title: "One or more validation errors occurred",
            statusCode: StatusCodes.Status400BadRequest,
            instance: null
        );
    }
}
```

#### 5.2: Error Response Mapping

**Ensure error responses match OpenAPI specification exactly**:

```csharp
// The OpenAPI spec defines these response codes that must be preserved:
// 200: Success
// 304: Not Modified (for GET with If-None-Match)
// 400: Bad Request (validation errors)
// 403: Forbidden (business rule violations)
// 404: Not Found (tenant not found)
// 405: Method Not Allowed (root tenant updates)
// 409: Conflict (tenant already exists)
// 422: Unprocessable Entity (invalid patch operations)
```

#### 5.2: Exception Handling

```csharp
// Middleware/TenancyExceptionHandler.cs
public class TenancyExceptionHandler : IExceptionHandler
{
    private readonly ILogger<TenancyExceptionHandler> logger;
    
    public TenancyExceptionHandler(ILogger<TenancyExceptionHandler> logger)
    {
        this.logger = logger;
    }
    
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception occurred");
        
        var result = exception switch
        {
            TenantNotFoundException => Results.NotFound(),
            TenantConflictException => Results.Conflict(),
            TenantNotModifiedException => Results.StatusCode(304),
            ArgumentException argEx => Results.BadRequest(argEx.Message),
            _ => Results.Problem("An error occurred while processing your request")
        };
        
        await result.ExecuteAsync(httpContext);
        return true;
    }
}
```

### Phase 6: OpenAPI and Documentation (Low Risk)

#### 6.1: Native OpenAPI Support

**Replace embedded YAML with code-first approach**:

```csharp
// Extensions/OpenApiExtensions.cs
public static class OpenApiExtensions
{
    public static IServiceCollection AddTenancyOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Tenancy Service",
                Version = "1.0.0",
                Description = "Marain tenant management API"
            });
            
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
        });
        
        return services;
    }
}
```

### Phase 7: Performance Optimization (Low Risk)

#### 7.1: Caching and Performance

```csharp
// Implement response caching
app.MapGet("/{tenantId}/marain/tenant", GetTenantAsync)
   .CacheOutput(policy => policy
       .SetVaryByRouteValue("tenantId")
       .Expire(TimeSpan.FromMinutes(5)));

// Add compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});
```

## Risk Assessment and Mitigation

### High Risk Areas

1. **API Contract Changes**: HAL to standard JSON responses
   - **Mitigation**: Implement HAL-compatible response format initially
   - **Rollback**: Feature flags to switch between formats

2. **Testing Migration**: Complete test rewrite required
   - **Mitigation**: Parallel testing during migration period
   - **Rollback**: Keep existing test infrastructure until confidence built

### Medium Risk Areas

1. **Link Generation**: Replacing Menes link resolver
   - **Mitigation**: Create compatibility layer using ASP.NET Core LinkGenerator
   
2. **Hosting Changes**: Azure Functions integration
   - **Mitigation**: Thorough testing in staging environment

### Low Risk Areas

1. **Service Registration**: DI configuration changes
2. **OpenAPI Documentation**: Moving from embedded YAML to code-first

## Migration Timeline

### Pre-Migration (Weeks 1-2)
- [ ] Set up feature branch
- [ ] Create project structure
- [ ] Implement base infrastructure

### Core Migration (Weeks 3-8)
- [ ] Implement Minimal API endpoints (Weeks 3-5)
- [ ] Migrate hosting layers (Weeks 6-7)
- [ ] Update testing infrastructure (Week 8)

### Testing and Validation (Weeks 9-11)
- [ ] Integration testing (Week 9)
- [ ] Performance testing (Week 10)
- [ ] Security validation (Week 11)

### Deployment and Monitoring (Week 12)
- [ ] Staged deployment
- [ ] Production rollout
- [ ] Post-deployment monitoring

## Success Criteria

### Functional Requirements
- [ ] All existing API endpoints work identically
- [ ] Response formats maintain backward compatibility
- [ ] Authentication and authorization preserved
- [ ] Error handling behavior unchanged

### Non-Functional Requirements
- [ ] Performance improvement: 15-20% faster response times
- [ ] Memory usage reduction: 30-40% less allocation
- [ ] Code maintainability: Reduced complexity metrics
- [ ] Test coverage: Maintain or improve current coverage

### Technical Requirements
- [ ] Remove all Menes dependencies
- [ ] Reduce NuGet package count by 50%
- [ ] Simplify hosting configuration
- [ ] Improve developer experience

## Rollback Strategy

1. **Feature Flag Approach**: Implement switches between Menes and Minimal APIs
2. **Blue-Green Deployment**: Maintain parallel deployments
3. **Database Compatibility**: Ensure no breaking changes to storage
4. **Client Compatibility**: Maintain API contract guarantees

## Post-Migration Activities

### Code Cleanup
- Remove unused Menes-related code
- Update documentation
- Archive old test infrastructure

### Performance Monitoring
- Establish new baselines
- Monitor error rates
- Track performance improvements

### Team Training
- Minimal APIs best practices
- New testing approaches
- Updated deployment procedures

## Conclusion

This migration plan provides a structured approach to moving from Menes to ASP.NET Core Minimal APIs while minimizing risk and maintaining system stability. The phased approach allows for validation at each step and provides clear rollback points if issues arise.

The expected benefits include improved performance, reduced complexity, better maintainability, and alignment with modern .NET development practices. The migration timeline of 12 weeks provides adequate time for thorough testing and validation while maintaining development velocity.