# ADR-0003: OpenAPI Schema Generation Alignment with Custom JSON Serialization

## Status

Proposed

## Context

The Marain.Tenancy.MinimalApi uses custom JsonConverters from the Corvus.Json.Serialization library to provide specialized serialization behavior for specific types:

1. **DateTimeOffset** - Serialized as `{ "dateTimeOffset": "ISO8601", "unixTime": 123456789 }`
2. **CultureInfo** - Serialized as simple string (e.g., `"en-GB"`) instead of full object
3. **IPropertyBag** - Serialized as dynamic object with arbitrary properties

These converters work correctly at runtime, as evidenced by the `/debug/serialization-demo` endpoint output. However, the OpenAPI/Swagger schema generation at `/swagger/v1/swagger.json` does not reflect this custom serialization behavior:

- **DateTimeOffset** appears as `"type": "string", "format": "date-time"` instead of the dual-property object
- **CultureInfo** appears as complex object with all .NET properties instead of simple string  
- **IPropertyBag** appears as `{"type": "object", "additionalProperties": false}` instead of allowing dynamic properties

### Root Cause

OpenAPI schema generation uses .NET type introspection at compile-time, while JsonConverters only affect runtime serialization behavior. Swashbuckle generates schemas based on the actual .NET type definitions, completely ignoring the configured JsonConverter customizations.

### Impact

- **API Documentation Accuracy**: Consumers see incorrect schema expectations
- **Client SDK Generation**: Generated clients may have wrong type definitions
- **Developer Experience**: Confusion between documented and actual API behavior
- **Testing**: Integration tests may fail due to schema/reality mismatch

## Decision Drivers

- **Accuracy**: Schema must accurately represent actual API behavior
- **Maintainability**: Solution should minimize risk of schema-serialization drift
- **Complexity**: Implementation should be proportional to the problem scope
- **Performance**: Schema generation should remain fast
- **Extensibility**: Should accommodate future custom converters easily
- **Reliability**: Low risk of introducing bugs in critical API documentation

## Considered Options

### Option 1: Custom Schema Filters (ISchemaFilter)

**Implementation Approach:**
```csharp
public class CustomSerializationSchemaFilter : ISchemaFilter
{
    private readonly IJsonSerializerOptionsProvider _optionsProvider;

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(DateTimeOffset) || context.Type == typeof(DateTimeOffset?))
        {
            // Replace with dual-property object schema
            schema.Type = "object";
            schema.Properties = new Dictionary<string, OpenApiSchema>
            {
                ["dateTimeOffset"] = new() { Type = "string", Format = "date-time" },
                ["unixTime"] = new() { Type = "integer", Format = "int64" }
            };
        }
        // Similar logic for CultureInfo and IPropertyBag
    }
}
```

**Pros:**
- Fine-grained control over schema generation
- Can inspect actual JsonConverter configuration at runtime
- Flexible - handles complex scenarios and edge cases
- Non-intrusive - doesn't change core serialization setup
- Can potentially auto-detect converter presence

**Cons:**
- High implementation complexity
- Requires deep OpenAPI schema knowledge
- Manual maintenance needed when converters change
- Performance overhead during schema generation
- Risk of schema drift if filters aren't updated with converter changes
- Complex error handling and edge case management

**Complexity:** High  
**Maintenance Risk:** High

### Option 2: Static Schema Mappers (MapType<T>)

**Implementation Approach:**
```csharp
builder.Services.AddSwaggerGen(options =>
{
    // DateTimeOffset with custom converter schema
    options.MapType<DateTimeOffset>(() => new OpenApiSchema
    {
        Type = "object",
        Properties = new Dictionary<string, OpenApiSchema>
        {
            ["dateTimeOffset"] = new() { Type = "string", Format = "date-time" },
            ["unixTime"] = new() { Type = "integer", Format = "int64" }
        },
        Required = new HashSet<string> { "dateTimeOffset", "unixTime" }
    });

    // CultureInfo as simple string
    options.MapType<CultureInfo>(() => new OpenApiSchema
    {
        Type = "string",
        Description = "Culture identifier (e.g., 'en-GB', 'fr-FR')"
    });

    // IPropertyBag as dynamic object
    options.MapType<IPropertyBag>(() => new OpenApiSchema
    {
        Type = "object",
        AdditionalProperties = new OpenApiSchema { Type = "object" },
        Description = "Dynamic property bag containing key-value pairs"
    });
});
```

**Pros:**
- Simple and straightforward implementation
- Clear and explicit schema definitions
- Excellent performance (no runtime inspection)
- Easy to understand, review, and maintain
- Low risk of introducing bugs
- Predictable behavior

**Cons:**
- Static definitions - can't automatically detect converter changes
- Manual synchronization required between converters and schemas
- Risk of drift if converters are updated but schemas are not
- Less flexible for complex dynamic scenarios
- Requires developers to remember to update both places

**Complexity:** Low-Medium  
**Maintenance Risk:** Medium

### Option 3: Runtime JsonConverter-Integrated Schema Generation

**Implementation Approach:**
```csharp
public class JsonConverterAwareSchemaGenerator : ISchemaGenerator
{
    public OpenApiSchema GenerateSchema(Type type, SchemaRepository schemaRepository)
    {
        // Serialize sample instances using configured JsonConverters
        var sampleInstance = CreateSampleInstance(type);
        var jsonOptions = serviceProvider.GetService<IJsonSerializerOptionsProvider>();
        var serializedJson = JsonSerializer.Serialize(sampleInstance, jsonOptions.Instance);
        
        // Infer schema from actual serialized output
        return InferSchemaFromJson(serializedJson, type);
    }
}
```

**Pros:**
- Perfect synchronization between runtime serialization and schema
- Automatically adapts to JsonConverter changes
- No risk of drift between implementation and documentation
- Future-proof - new converters automatically reflected
- Single source of truth for serialization behavior

**Cons:**
- Very high implementation complexity
- Significant performance overhead during schema generation
- May not work for types requiring runtime context/dependencies
- Fragile if serialization has side effects or external dependencies
- Complex error handling for types that can't be easily instantiated
- Difficult to handle polymorphic types and generics
- May produce inconsistent results for non-deterministic serialization

**Complexity:** Very High  
**Maintenance Risk:** High (during development), Low (once stable)

### Option 4: Hybrid Schema Filters with Converter Detection

**Implementation Approach:**
```csharp
public class ConverterAwareSchemaFilter : ISchemaFilter
{
    private readonly IJsonSerializerOptionsProvider _optionsProvider;

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var converters = _optionsProvider.Instance.Converters;
        
        // Auto-detect presence of specific converters and apply schemas
        if (HasConverterForType<DateTimeOffset>(converters))
        {
            ApplyDateTimeOffsetSchema(schema, context);
        }
        // Similar auto-detection for other types
    }
}
```

**Pros:**
- Automatic detection of converter presence
- Flexible and extensible
- Better synchronization than pure manual approach
- Can handle future converters with generic logic

**Cons:**
- Still complex to implement correctly
- Requires converter introspection logic
- May not work for all converter types
- Performance overhead
- Complex testing and edge case handling

**Complexity:** High  
**Maintenance Risk:** Medium-High

## Decision Outcome

**Chosen Option: Option 2 - Static Schema Mappers (MapType<T>)**

### Rationale

After deep analysis, Option 2 provides the optimal balance of simplicity, reliability, and maintainability for this specific context:

1. **Scope is Limited**: Only 3 types need custom schema handling, making static definitions manageable

2. **Stable Requirements**: The custom serialization behavior for these types is well-established and unlikely to change frequently

3. **Risk Management**: Simple implementation minimizes the chance of introducing bugs in critical API documentation

4. **Performance**: No runtime overhead during schema generation, which is important for developer experience

5. **Team Capability**: Easy for any team member to understand, maintain, and extend

6. **Pragmatic**: Addresses the immediate problem without over-engineering

### Implementation Strategy

1. **Immediate Implementation**:
   - Add MapType<T> calls for DateTimeOffset, CultureInfo, and IPropertyBag in Swagger configuration
   - Document the schema mappings with comments explaining the JsonConverter alignment

2. **Process Integration**:
   - Add code review checklist item: "If JsonConverters are modified, update corresponding OpenAPI schema mappings"
   - Include schema validation in integration tests to catch drift

3. **Future Migration Path**:
   - If the number of custom types grows significantly (>10), consider migrating to Option 1 or 4
   - Monitor for schema-serialization drift during code reviews

### Validation Approach

```csharp
[Test]
public async Task SerializationMatchesSchema()
{
    // Test that actual serialization matches documented schema
    var testData = new { 
        dateTime = DateTimeOffset.Now, 
        culture = CultureInfo.CurrentCulture 
    };
    
    var actualJson = JsonSerializer.Serialize(testData, jsonOptions);
    var swaggerSchema = await GetSwaggerSchema("/debug/serialization-demo");
    
    // Validate that actualJson conforms to swaggerSchema
    ValidateJsonAgainstSchema(actualJson, swaggerSchema);
}
```

## Pros and Cons of the Outcome

### Pros
- **Quick Implementation**: Can be completed in a single development session
- **Low Risk**: Minimal chance of introducing regressions
- **Clear Ownership**: Easy to see which schemas correspond to which converters  
- **Good Performance**: No impact on schema generation speed
- **Maintainable**: Straightforward to modify or extend

### Cons
- **Manual Synchronization**: Developers must remember to update both converters and schemas
- **Drift Risk**: Schemas could become outdated if converters change
- **Limited Flexibility**: Can't handle dynamic converter scenarios
- **Process Dependency**: Relies on code review discipline to prevent drift

## Links

- [Swashbuckle MapType Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore#override-schema-for-specific-types)
- [ADR-0002: Separation of Read and Modify](./0002-separation-of-read-and-modify.md)
- [Corvus.Json.Serialization Documentation](https://github.com/corvus-dotnet/Corvus.JsonSchema)
- [OpenAPI 3.0 Schema Specification](https://spec.openapis.org/oas/v3.0.3#schema-object)