// <copyright file="MinimalApiExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Extensions;

using System.Collections.Frozen;
using FluentValidation;
using Marain.Tenancy.MinimalApi.Validation;

/// <summary>
/// Extension methods for configuring Minimal API services and endpoints.
/// </summary>
public static class MinimalApiExtensions
{
    /// <summary>
    /// Content types allowed for different HTTP methods.
    /// </summary>
    private static readonly FrozenDictionary<string, string[]> AllowedContentTypes = 
        new Dictionary<string, string[]>
        {
            ["PATCH"] = ["application/json-patch+json"],
            ["POST"] = ["application/json"],
            ["PUT"] = ["application/json"]
        }.ToFrozenDictionary();

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
                Description = "Marain tenant management API"
            });
        });
        
        builder.Services.AddValidatorsFromAssemblyContaining<TenantParametersValidator>();
        
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
        
        var tenants = app.MapGroup("/{tenantId}/marain/tenant")
            .WithTags("Tenancy")
            .WithOpenApi();
            
        tenants.RegisterTenantEndpoints();
        
        return app;
    }
}