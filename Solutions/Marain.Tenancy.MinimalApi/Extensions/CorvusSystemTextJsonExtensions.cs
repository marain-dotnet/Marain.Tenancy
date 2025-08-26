// <copyright file="CorvusSystemTextJsonExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Extensions;

using System.Globalization;
using Corvus.Json;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

/// <summary>
/// Extension methods to support code from Covus.Extensions.System.Text.Json.
/// </summary>
public static class CorvusSystemTextJsonExtensions
{
    /// <summary>
    /// When using the <see cref="CorvusJsonSerializationServiceCollectionExtensions.AddJsonDateTimeOffsetToIso8601AndUnixTimeConverter(IServiceCollection)"/>
    /// method to add custom serialization for DateTimeOffset instances, this method can be used to configure Swagger generation with the corresponding
    /// custom schema.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to configure.</param>
    public static void AddJsonDateTimeOffsetToIso8601AndUnixTimeStampConverterSwaggerGen(this SwaggerGenOptions options)
    {
        // DateTimeOffset: Corvus JsonDateTimeOffsetToIso8601AndUnixTimeConverter
        // serializes as { "dateTimeOffset": "ISO8601", "unixTime": 123456789 }
        options.MapType<DateTimeOffset>(() => new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["dateTimeOffset"] = new() { Type = "string", Format = "date-time", Description = "ISO 8601 formatted date-time string" },
                ["unixTime"] = new() { Type = "integer", Format = "int64", Description = "Unix timestamp in milliseconds" },
            },
            Required = new HashSet<string> { "dateTimeOffset", "unixTime" },
            Description = "DateTimeOffset serialized with both ISO 8601 and Unix timestamp formats",
        });

        // DateTimeOffset?: Nullable version with same schema
        options.MapType<DateTimeOffset?>(() => new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["dateTimeOffset"] = new() { Type = "string", Format = "date-time", Description = "ISO 8601 formatted date-time string" },
                ["unixTime"] = new() { Type = "integer", Format = "int64", Description = "Unix timestamp in milliseconds" },
            },
            Required = new HashSet<string> { "dateTimeOffset", "unixTime" },
            Description = "DateTimeOffset serialized with both ISO 8601 and Unix timestamp formats",
            Nullable = true,
        });
    }

    /// <summary>
    /// When using the <see cref="CorvusJsonSerializationServiceCollectionExtensions.AddJsonCultureInfoConverter(IServiceCollection)"/>
    /// method to add custom serialization for CultureInfo instances, this method can be used to configure Swagger generation with the corresponding
    /// custom schema.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to configure.</param>
    public static void AddJsonCultureInfoConverterSwaggerGen(this SwaggerGenOptions options)
    {
        // CultureInfo: Corvus JsonCultureInfoConverter serializes as simple string (e.g., "en-GB")
        options.MapType<CultureInfo>(() => new OpenApiSchema
        {
            Type = "string",
            Description = "Culture identifier (e.g., 'en-GB', 'fr-FR', 'en-US')",
            Example = new Microsoft.OpenApi.Any.OpenApiString("en-GB"),
        });
    }

    /// <summary>
    /// When using the <see cref="CorvusJsonPropertyBagSerializationServiceCollectionExtensions.AddJsonPropertyBagFactory(IServiceCollection)"/>
    /// method to add support for IPropertyBag, this method can be used to configure Swagger generation with the corresponding
    /// custom schema.
    /// </summary>
    /// <param name="options">The <see cref="SwaggerGenOptions"/> to configure.</param>
    public static void AddJsonPropertyBagConverterSwaggerGen(this SwaggerGenOptions options)
    {
        options.MapType<IPropertyBag>(() => new OpenApiSchema
        {
            Type = "object",
            AdditionalProperties = new OpenApiSchema
            {
                Description = "Any valid JSON value (string, number, boolean, object, array, or null)",
            },
            Description = "Dynamic property bag containing key-value pairs of arbitrary JSON data",
            Example = new Microsoft.OpenApi.Any.OpenApiObject
            {
                ["stringProperty"] = new Microsoft.OpenApi.Any.OpenApiString("example"),
                ["numberProperty"] = new Microsoft.OpenApi.Any.OpenApiInteger(42),
                ["booleanProperty"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true),
            },
        });
    }
}