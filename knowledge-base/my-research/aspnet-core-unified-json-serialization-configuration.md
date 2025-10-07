# ASP.NET Core Unified JSON Serialization Configuration

## Research Question
How to ensure a custom `JsonSerializerOptions` from DI container (via `JsonSerializerOptionsProvider`) is used consistently across all JSON serialization contexts in an ASP.NET Core Minimal API application.

## The Challenge

ASP.NET Core has multiple independent serialization contexts:

1. **Minimal API endpoints** - Uses `HttpJsonOptions`
2. **MVC Controllers** - Uses `JsonOptions` 
3. **Manual serialization** - Uses default `JsonSerializerOptions`
4. **SignalR** - Has its own JSON options
5. **Problem Details** - Separate configuration
6. **OpenAPI/Swagger** - May have different serialization for schema generation

Each context requires separate configuration to ensure consistency.

## Current Implementation Analysis

In `Program.cs` lines 61-64, the code:

```csharp
// Line 61: Add JsonSerializerOptionsProvider to DI
builder.Services.AddJsonSerializerOptionsProvider();

// Lines 62-64: Add custom converters via Corvus.Json.Serialization extensions
builder.Services.AddJsonCultureInfoConverter();
builder.Services.AddJsonDateTimeOffsetToIso8601AndUnixTimeConverter();
builder.Services.AddCamelCaseConverterForEnums();
```

However, these custom options are **not automatically applied** to all ASP.NET Core serialization contexts.

## Solution: Unified Configuration Strategy

### 1. Create Configuration Helper Method

```csharp
private static void ConfigureJsonSerializerOptions(JsonSerializerOptions options, IServiceProvider serviceProvider)
{
    // Get the customized options from DI
    var optionsProvider = serviceProvider.GetRequiredService<JsonSerializerOptionsProvider>();
    var customOptions = optionsProvider.GetJsonSerializerOptions();
    
    // Copy all converters from custom options
    foreach (var converter in customOptions.Converters)
    {
        if (!options.Converters.Any(c => c.GetType() == converter.GetType()))
        {
            options.Converters.Add(converter);
        }
    }
    
    // Copy other settings
    options.PropertyNamingPolicy = customOptions.PropertyNamingPolicy;
    options.DictionaryKeyPolicy = customOptions.DictionaryKeyPolicy;
    options.WriteIndented = customOptions.WriteIndented;
    options.DefaultIgnoreCondition = customOptions.DefaultIgnoreCondition;
    options.PropertyNameCaseInsensitive = customOptions.PropertyNameCaseInsensitive;
    options.ReadCommentHandling = customOptions.ReadCommentHandling;
    options.AllowTrailingCommas = customOptions.AllowTrailingCommas;
}
```

### 2. Apply to All Contexts

```csharp
// Build the service provider first to access JsonSerializerOptionsProvider
var serviceProvider = builder.Services.BuildServiceProvider();

// Configure Minimal API JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    ConfigureJsonSerializerOptions(options.SerializerOptions, serviceProvider);
});

// Configure MVC Controller JSON options
builder.Services.Configure<JsonOptions>(options =>
{
    ConfigureJsonSerializerOptions(options.SerializerOptions, serviceProvider);
});

// Configure Problem Details JSON options
builder.Services.Configure<ProblemDetailsOptions>(options =>
{
    // Problem details uses its own serializer context
    // May need custom configuration here
});
```

### 3. Alternative: Extension Method Approach

Create an extension method for cleaner implementation:

```csharp
public static class JsonConfigurationExtensions
{
    public static WebApplicationBuilder ConfigureUnifiedJsonSerialization(this WebApplicationBuilder builder)
    {
        // Configure all JSON contexts to use the same options
        builder.Services.PostConfigure<HttpJsonOptions>(options =>
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            ConfigureJsonSerializerOptions(options.SerializerOptions, serviceProvider);
        });
        
        builder.Services.PostConfigure<JsonOptions>(options =>
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            ConfigureJsonSerializerOptions(options.SerializerOptions, serviceProvider);
        });
        
        return builder;
    }
}
```

### 4. Manual Serialization Context

For manual `JsonSerializer` calls, inject the provider:

```csharp
app.MapGet("/custom-serialization", (JsonSerializerOptionsProvider optionsProvider) =>
{
    var data = new { message = "test" };
    var json = JsonSerializer.Serialize(data, optionsProvider.GetJsonSerializerOptions());
    return Results.Content(json, "application/json");
});
```

## Specific Implementation for Current Codebase

### Current Problem Analysis

The current `Program.cs` setup:

```csharp
// Lines 50-53: Configure HttpJsonOptions (Minimal API)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
});

// Lines 55-58: Configure JsonOptions (MVC Controllers)
builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
});

// Lines 61-64: Add Corvus converters to DI (NOT applied to above configs)
builder.Services.AddJsonSerializerOptionsProvider();
builder.Services.AddJsonCultureInfoConverter();
builder.Services.AddJsonDateTimeOffsetToIso8601AndUnixTimeConverter();
builder.Services.AddCamelCaseConverterForEnums();
```

**The Issue**: Corvus converters are registered in DI but not automatically applied to ASP.NET Core's JSON configurations.

### Recommended Solution: Replace Current Configuration

Replace lines 50-64 in `Program.cs` with:

```csharp
// First, register all the Corvus serialization components
builder.Services.AddJsonSerializerOptionsProvider();
builder.Services.AddJsonCultureInfoConverter();
builder.Services.AddJsonDateTimeOffsetToIso8601AndUnixTimeConverter();
builder.Services.AddCamelCaseConverterForEnums();

// Then configure ASP.NET Core to use the DI-provided options
builder.Services.PostConfigure<HttpJsonOptions>((options, serviceProvider) =>
{
    var optionsProvider = serviceProvider.GetRequiredService<JsonSerializerOptionsProvider>();
    var customOptions = optionsProvider.GetJsonSerializerOptions();
    
    // Clear any existing converters to avoid duplicates
    options.SerializerOptions.Converters.Clear();
    
    // Add all converters from the DI-configured options
    foreach (var converter in customOptions.Converters)
    {
        options.SerializerOptions.Converters.Add(converter);
    }
    
    // Apply other settings
    options.SerializerOptions.PropertyNamingPolicy = customOptions.PropertyNamingPolicy;
    options.SerializerOptions.DictionaryKeyPolicy = customOptions.DictionaryKeyPolicy;
    options.SerializerOptions.WriteIndented = customOptions.WriteIndented;
    options.SerializerOptions.DefaultIgnoreCondition = customOptions.DefaultIgnoreCondition;
    options.SerializerOptions.PropertyNameCaseInsensitive = customOptions.PropertyNameCaseInsensitive;
});

builder.Services.PostConfigure<JsonOptions>((options, serviceProvider) =>
{
    var optionsProvider = serviceProvider.GetRequiredService<JsonSerializerOptionsProvider>();
    var customOptions = optionsProvider.GetJsonSerializerOptions();
    
    // Clear any existing converters to avoid duplicates
    options.JsonSerializerOptions.Converters.Clear();
    
    // Add all converters from the DI-configured options
    foreach (var converter in customOptions.Converters)
    {
        options.JsonSerializerOptions.Converters.Add(converter);
    }
    
    // Apply other settings
    options.JsonSerializerOptions.PropertyNamingPolicy = customOptions.PropertyNamingPolicy;
    options.JsonSerializerOptions.DictionaryKeyPolicy = customOptions.DictionaryKeyPolicy;
    options.JsonSerializerOptions.WriteIndented = customOptions.WriteIndented;
    options.JsonSerializerOptions.DefaultIgnoreCondition = customOptions.DefaultIgnoreCondition;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = customOptions.PropertyNameCaseInsensitive;
});
```

### Key Changes Explained

1. **Order Change**: Corvus services are registered first
2. **PostConfigure**: Uses `PostConfigure` with service provider injection
3. **Clear First**: Clears existing converters to avoid duplicates
4. **Complete Copy**: Copies all settings, not just converters
5. **Single Source**: Both configurations use the same `JsonSerializerOptionsProvider`

### Validation Approach

Add this endpoint to test unified serialization:

```csharp
app.MapGet("/debug/serialization-test", (JsonSerializerOptionsProvider optionsProvider) =>
{
    var testData = new
    {
        enumValue = SomeEnum.TestValue,
        dateTime = DateTimeOffset.Now,
        culture = CultureInfo.CurrentCulture.Name
    };
    
    // Test DI options
    var diJson = JsonSerializer.Serialize(testData, optionsProvider.GetJsonSerializerOptions());
    
    // Return both for comparison (ASP.NET Core will use configured options for response)
    return Results.Json(new { 
        diSerialization = diJson, 
        frameworkData = testData 
    });
});
```

This allows you to verify that both manual serialization and framework serialization produce identical results.

## ✅ IMPLEMENTATION STATUS

**UPDATE**: This solution has been implemented in the codebase.

### Implemented Solution

The unified JSON serialization has been implemented via:

1. **Extension Method**: `ConfigureUnifiedJsonSerialization()` in `MinimalApiExtensions.cs` (lines 71-92)
2. **Helper Method**: `ConfigureJsonSerializerOptions()` in `MinimalApiExtensions.cs` (lines 100-123)  
3. **Integration**: Called from `Program.cs` line 49: `builder.ConfigureUnifiedJsonSerialization();`

### Key Features of Implementation

1. **Correct Order**: Corvus services registered first
2. **PostConfigure Usage**: Uses `PostConfigure` to ensure services are available
3. **Duplicate Prevention**: Checks for existing converters before adding
4. **Complete Configuration**: Copies all relevant JsonSerializerOptions settings
5. **Both Contexts**: Configures both HTTP JSON (Minimal API) and MVC JSON options
6. **Type Safety**: Uses fully qualified type names to avoid conflicts

### Verification Steps

To verify the unified serialization is working:

1. **Test Different Contexts**: Create endpoints that use both framework serialization and manual serialization
2. **Check Swagger Output**: Verify Swagger reflects the custom enum serialization
3. **Compare Results**: Ensure consistent output across all serialization contexts

Example test endpoint:
```csharp
app.MapGet("/debug/serialization-test", (IJsonSerializerOptionsProvider optionsProvider) =>
{
    var testData = new
    {
        enumValue = UpdateTenantJsonPatchEntryOperation.Add,
        dateTime = DateTimeOffset.Now,
        culture = CultureInfo.CurrentCulture.Name
    };
    
    // Test manual serialization using DI options
    var diJson = JsonSerializer.Serialize(testData, optionsProvider.Instance);
    
    // Framework will use configured options for this response
    return Results.Json(new { 
        manualSerialization = diJson, 
        frameworkSerialization = testData 
    });
});
```

Both `manualSerialization` and `frameworkSerialization` should produce identical JSON output.

## Important Considerations

### 1. Service Provider Building
Building the service provider mid-configuration can have implications. Consider using `PostConfigure` instead of `Configure` to ensure all services are registered first.

### 2. Converter Duplication
Check for duplicate converters when copying to avoid conflicts.

### 3. Order of Operations
Ensure the Corvus extensions are registered before attempting to use the JsonSerializerOptionsProvider.

### 4. Testing Strategy
Test serialization in all contexts:
- Minimal API responses
- Manual JsonSerializer calls
- Swagger schema generation
- Error responses (ProblemDetails)

## Alternative: Factory Pattern

For more complex scenarios, consider a factory pattern:

```csharp
public interface IJsonSerializerOptionsFactory
{
    JsonSerializerOptions GetOptions();
}

public class CustomJsonSerializerOptionsFactory : IJsonSerializerOptionsFactory
{
    private readonly JsonSerializerOptionsProvider _provider;
    
    public CustomJsonSerializerOptionsFactory(JsonSerializerOptionsProvider provider)
    {
        _provider = provider;
    }
    
    public JsonSerializerOptions GetOptions()
    {
        return _provider.GetJsonSerializerOptions();
    }
}
```

## Conclusion

To ensure unified JSON serialization:

1. Use `PostConfigure` for both `HttpJsonOptions` and `JsonOptions`
2. Extract options from DI-registered `JsonSerializerOptionsProvider`
3. Copy converters and settings to framework options
4. Test all serialization contexts
5. Consider manual serialization scenarios

The key insight is that ASP.NET Core's multiple serialization contexts require explicit coordination - there's no single configuration point that affects everything.