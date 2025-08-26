// <copyright file="SerializationSchemaAlignmentTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Tests.Integration;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

/// <summary>
/// Integration tests to verify that OpenAPI schema documentation matches actual JSON serialization behavior.
/// Validates that custom JsonConverters are correctly reflected in the generated OpenAPI schemas.
/// </summary>
[TestClass]
public sealed class SerializationSchemaAlignmentTests : IDisposable
{
    private readonly TenancyApiTestWebApplicationFactory factory;
    private readonly HttpClient client;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializationSchemaAlignmentTests"/> class.
    /// </summary>
    public SerializationSchemaAlignmentTests()
    {
        this.factory = new TenancyApiTestWebApplicationFactory();
        this.client = this.factory.CreateClient();
    }

    /// <summary>
    /// Verifies that the /debug/serialization-demo endpoint produces JSON that matches the OpenAPI schema.
    /// This test validates that custom JsonConverters are properly reflected in schema generation.
    /// </summary>
    [TestMethod]
    public async Task SerializationDemo_JsonOutput_MatchesOpenApiSchema()
    {
        // Arrange: Get the actual JSON response from the endpoint
        HttpResponseMessage response = await this.client.GetAsync("/debug/serialization-demo");
        response.EnsureSuccessStatusCode();

        string actualJson = await response.Content.ReadAsStringAsync();
        JsonDocument actualJsonDoc = JsonDocument.Parse(actualJson);

        // Arrange: Get the OpenAPI schema for this endpoint
        HttpResponseMessage swaggerResponse = await this.client.GetAsync("/swagger/v1/swagger.json");
        swaggerResponse.EnsureSuccessStatusCode();

        string swaggerJson = await swaggerResponse.Content.ReadAsStringAsync();
        OpenApiDocument openApiDoc = new OpenApiStringReader().Read(swaggerJson, out _);

        // Get the schema for the serialization demo endpoint
        OpenApiPathItem pathItem = openApiDoc.Paths["/debug/serialization-demo"];
        OpenApiResponse okResponse = pathItem.Operations[OperationType.Get].Responses["200"];
        OpenApiSchema responseSchema = okResponse.Content["application/json"].Schema;

        // Act & Assert: Validate the structure matches expectations
        this.ValidateSerializationDemoSchema(actualJsonDoc.RootElement, responseSchema, openApiDoc);
    }

    /// <summary>
    /// Specifically tests DateTimeOffset serialization format alignment.
    /// Validates dual-property format: { "dateTimeOffset": "ISO8601", "unixTime": 123456789 }.
    /// </summary>
    [TestMethod]
    public async Task SerializationDemo_DateTimeOffset_HasCorrectDualFormat()
    {
        // Arrange
        HttpResponseMessage response = await this.client.GetAsync("/debug/serialization-demo");
        response.EnsureSuccessStatusCode();

        string jsonContent = await response.Content.ReadAsStringAsync();
        JsonDocument document = JsonDocument.Parse(jsonContent);

        // Act: Extract dateTime property
        JsonElement dateTimeElement = document.RootElement.GetProperty("dateTime");

        // Assert: Verify dual-property structure
        dateTimeElement.ValueKind.ShouldBe(JsonValueKind.Object);
        dateTimeElement.TryGetProperty("dateTimeOffset", out JsonElement isoProperty).ShouldBeTrue();
        dateTimeElement.TryGetProperty("unixTime", out JsonElement unixProperty).ShouldBeTrue();

        // Assert: Verify property types
        isoProperty.ValueKind.ShouldBe(JsonValueKind.String);
        unixProperty.ValueKind.ShouldBe(JsonValueKind.Number);

        // Assert: Verify ISO8601 format
        string isoString = isoProperty.GetString()!;
        DateTimeOffset.TryParse(isoString, out _).ShouldBeTrue();

        // Assert: Verify Unix timestamp is reasonable (positive number)
        long unixTime = unixProperty.GetInt64();
        unixTime.ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// Specifically tests CultureInfo serialization format alignment.
    /// Validates simple string format instead of complex object.
    /// </summary>
    [TestMethod]
    public async Task SerializationDemo_CultureInfo_IsSimpleString()
    {
        // Arrange
        HttpResponseMessage response = await this.client.GetAsync("/debug/serialization-demo");
        response.EnsureSuccessStatusCode();

        string jsonContent = await response.Content.ReadAsStringAsync();
        JsonDocument document = JsonDocument.Parse(jsonContent);

        // Act: Extract culture property
        JsonElement cultureElement = document.RootElement.GetProperty("culture");

        // Assert: Verify it's a simple string, not a complex object
        cultureElement.ValueKind.ShouldBe(JsonValueKind.String);
        
        string cultureString = cultureElement.GetString()!;
        cultureString.ShouldNotBeNullOrWhiteSpace();
        
        // Verify it's a valid culture identifier format
        CultureInfo.GetCultureInfo(cultureString).ShouldNotBeNull();
    }

    /// <summary>
    /// Specifically tests IPropertyBag serialization format alignment.
    /// Validates dynamic object with arbitrary properties.
    /// </summary>
    [TestMethod]
    public async Task SerializationDemo_PropertyBag_AllowsArbitraryProperties()
    {
        // Arrange
        HttpResponseMessage response = await this.client.GetAsync("/debug/serialization-demo");
        response.EnsureSuccessStatusCode();

        string jsonContent = await response.Content.ReadAsStringAsync();
        JsonDocument document = JsonDocument.Parse(jsonContent);

        // Act: Extract propertyBag property
        JsonElement propertyBagElement = document.RootElement.GetProperty("propertyBag");

        // Assert: Verify it's an object
        propertyBagElement.ValueKind.ShouldBe(JsonValueKind.Object);

        // Assert: Verify it contains the expected demo properties
        var expectedProperties = new Dictionary<string, JsonValueKind>
        {
            ["string-demo"] = JsonValueKind.String,
            ["int-demo"] = JsonValueKind.Number,
            ["decimal-demo"] = JsonValueKind.Number,
            ["datetimeoffset-demo"] = JsonValueKind.Object, // Should also use dual-format converter
            ["nestedobject-demo"] = JsonValueKind.Object,
        };

        foreach (KeyValuePair<string, JsonValueKind> expected in expectedProperties)
        {
            propertyBagElement.TryGetProperty(expected.Key, out JsonElement property).ShouldBeTrue();
            property.ValueKind.ShouldBe(expected.Value);
        }

        // Assert: Verify nested DateTimeOffset also uses dual format
        JsonElement nestedDateTime = propertyBagElement.GetProperty("datetimeoffset-demo");
        nestedDateTime.TryGetProperty("dateTimeOffset", out _).ShouldBeTrue();
        nestedDateTime.TryGetProperty("unixTime", out _).ShouldBeTrue();
    }

    /// <summary>
    /// Validates OpenAPI schema structure matches actual JSON response structure.
    /// </summary>
    /// <param name="jsonElement">The actual JSON response element.</param>
    /// <param name="schema">The OpenAPI schema to validate against.</param>
    /// <param name="openApiDoc">The full OpenAPI document for reference resolution.</param>
    private void ValidateSerializationDemoSchema(JsonElement jsonElement, OpenApiSchema schema, OpenApiDocument openApiDoc)
    {
        // Follow schema reference if needed
        if (!string.IsNullOrEmpty(schema.Reference?.Id))
        {
            string schemaName = schema.Reference.Id;
            schema = openApiDoc.Components.Schemas[schemaName];
        }

        // Validate object structure
        jsonElement.ValueKind.ShouldBe(JsonValueKind.Object);
        schema.Type.ShouldBe("object");

        // Validate that all JSON properties have corresponding schema properties
        foreach (JsonProperty jsonProperty in jsonElement.EnumerateObject())
        {
            schema.Properties.ShouldContainKey(jsonProperty.Name, 
                $"OpenAPI schema missing property: {jsonProperty.Name}");

            OpenApiSchema propertySchema = schema.Properties[jsonProperty.Name];

            // Validate specific property alignments
            switch (jsonProperty.Name)
            {
                case "dateTime":
                    this.ValidateDateTimeOffsetSchema(jsonProperty.Value, propertySchema, openApiDoc);
                    break;
                case "culture":
                    this.ValidateCultureInfoSchema(jsonProperty.Value, propertySchema);
                    break;
                case "propertyBag":
                    this.ValidatePropertyBagSchema(jsonProperty.Value, propertySchema);
                    break;
            }
        }
    }

    private void ValidateDateTimeOffsetSchema(JsonElement jsonElement, OpenApiSchema schema, OpenApiDocument openApiDoc)
    {
        // Follow reference if needed
        if (!string.IsNullOrEmpty(schema.Reference?.Id))
        {
            schema = openApiDoc.Components.Schemas[schema.Reference.Id];
        }

        // Should be object with dateTimeOffset and unixTime properties
        schema.Type.ShouldBe("object");
        schema.Properties.ShouldContainKey("dateTimeOffset");
        schema.Properties.ShouldContainKey("unixTime");

        jsonElement.ValueKind.ShouldBe(JsonValueKind.Object);
        jsonElement.TryGetProperty("dateTimeOffset", out _).ShouldBeTrue();
        jsonElement.TryGetProperty("unixTime", out _).ShouldBeTrue();
    }

    private void ValidateCultureInfoSchema(JsonElement jsonElement, OpenApiSchema schema)
    {
        // Should be simple string
        schema.Type.ShouldBe("string");
        jsonElement.ValueKind.ShouldBe(JsonValueKind.String);
    }

    private void ValidatePropertyBagSchema(JsonElement jsonElement, OpenApiSchema schema)
    {
        // Should be object with additionalProperties allowed
        schema.Type.ShouldBe("object");
        schema.AdditionalProperties.ShouldNotBeNull();
        jsonElement.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.client.Dispose();
        this.factory.Dispose();
    }
}