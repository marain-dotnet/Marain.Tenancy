// <copyright file="MinimalApiExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Extensions;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Corvus.Json;
using Corvus.Json.Serialization;
using FluentValidation;
using Marain.Tenancy.MinimalApi.Endpoints;
using Marain.Tenancy.MinimalApi.ErrorHandling;
using Marain.Tenancy.MinimalApi.Models;
using Marain.Tenancy.MinimalApi.Validation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;

/// <summary>
/// Extension methods for configuring Minimal API services and endpoints.
/// </summary>
public static class MinimalApiExtensions
{
    /// <summary>
    /// Adds tenancy Minimal API services to the application builder.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder AddTenancyMinimalApi(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "Tenancy Service",
                Version = "1.0.0",
                Description = "Marain tenant management API",
            });

            options.SupportNonNullableReferenceTypes();
        });

        builder.Services.AddValidatorsFromAssemblyContaining<CreateChildTenantParametersValidator>();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        // Note: ITenantStore should be registered by the hosting application
        // using AddTenantStoreOnAzureBlobStorage() or similar extension method
        return builder;
    }

    /// <summary>
    /// Maps tenancy endpoints to the web application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapTenancyEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        RouteGroupBuilder tenants = app.MapGroup("/{tenantId}/marain/tenant")
            .WithTags("Tenancy");

        tenants.RegisterTenantEndpoints();

        return app;
    }

    /// <summary>
    /// Maps the /swagger endpoint, as this is where we previously served the OpenApi definition from. This is done as a separate
    /// endpoint rather than a rewrite to ensure that the new endpoint itself appears in the swagger definition.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapCustomSwaggerEndpoint(this WebApplication app)
    {
        app.MapGet("/swagger", async (HttpContext context, IServiceProvider serviceProvider) =>
        {
            ISwaggerProvider swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();
            OpenApiDocument swagger = swaggerProvider.GetSwagger("v1");

            using var stringWriter = new StringWriter();
            swagger.SerializeAsV3(new OpenApiJsonWriter(stringWriter));
            string json = stringWriter.ToString();

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        });

        return app;
    }

    /// <summary>
    /// Maps the serialization demo endpoint.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication MapSerializationDemoEndpoint(this WebApplication app)
    {
        app.MapGet("/debug/serialization-demo", (IPropertyBagFactory propertyBagFactory) =>
        {
            Dictionary<string, object> propertyBagData = new()
            {
                ["string-demo"] = "string",
                ["int-demo"] = 4,
                ["decimal-demo"] = 56.32m,
                ["datetimeoffset-demo"] = DateTimeOffset.UtcNow,
                ["nestedobject-demo"] = new { Property1 = "One", Property2 = 2 },
            };

            var testData = new
            {
                DateTime = DateTimeOffset.Now,
                Culture = CultureInfo.CurrentCulture,
                PropertyBag = propertyBagFactory.Create(propertyBagData),
            };

            // Return both for comparison (ASP.NET Core will use configured options for response)
            return TypedResults.Ok(testData);
        });

        return app;
    }

    /// <summary>
    /// Configures both HTTP and MVC Json serialization.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> to configure.</param>
    /// <returns>The <see cref="WebApplicationBuilder"/> for chaining.</returns>
    public static WebApplicationBuilder ConfigureUnifiedJsonSerialization(this WebApplicationBuilder builder)
    {
        builder.Services.AddJsonSerializerOptionsProvider();
        builder.Services.AddJsonCultureInfoConverter();
        builder.Services.AddJsonDateTimeOffsetToIso8601AndUnixTimeConverter();
        builder.Services.AddCamelCaseConverterForEnums();
        builder.Services.AddJsonPropertyBagFactory();

        // Configure all JSON contexts to use the same options
        builder.Services.PostConfigure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
        {
            ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
            ConfigureJsonSerializerOptions(options.SerializerOptions, serviceProvider);
        });

        builder.Services.PostConfigure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
        {
            ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
            ConfigureJsonSerializerOptions(options.JsonSerializerOptions, serviceProvider);
        });

        return builder;
    }

    /// <summary>
    /// Configures the provided <see cref="JsonSerializerOptions" /> using options obtained from the registered
    /// <see cref="IJsonSerializerOptionsProvider"/>.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to configure.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> that will be used to obtain the <see cref="IJsonSerializerOptionsProvider"/>.</param>
    public static void ConfigureJsonSerializerOptions(JsonSerializerOptions options, IServiceProvider serviceProvider)
    {
        // Get the customized options from DI
        IJsonSerializerOptionsProvider optionsProvider = serviceProvider.GetRequiredService<IJsonSerializerOptionsProvider>();
        JsonSerializerOptions customOptions = optionsProvider.Instance;

        // Copy all converters from custom options
        foreach (JsonConverter converter in customOptions.Converters)
        {
            if (!options.Converters.Any(c => c.GetType() == converter.GetType()))
            {
                options.Converters.Add(converter);
            }
        }

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));

        // Copy other settings
        options.PropertyNamingPolicy = customOptions.PropertyNamingPolicy;
        options.DictionaryKeyPolicy = customOptions.DictionaryKeyPolicy;
        options.WriteIndented = customOptions.WriteIndented;
        options.DefaultIgnoreCondition = customOptions.DefaultIgnoreCondition;
        options.PropertyNameCaseInsensitive = customOptions.PropertyNameCaseInsensitive;
        options.ReadCommentHandling = customOptions.ReadCommentHandling;
        options.AllowTrailingCommas = customOptions.AllowTrailingCommas;
    }
}