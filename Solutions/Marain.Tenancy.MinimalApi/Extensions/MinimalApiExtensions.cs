// <copyright file="MinimalApiExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Extensions;

using FluentValidation;
using Marain.Tenancy.MinimalApi.Endpoints;
using Marain.Tenancy.MinimalApi.ErrorHandling;
using Marain.Tenancy.MinimalApi.Validation;

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
}