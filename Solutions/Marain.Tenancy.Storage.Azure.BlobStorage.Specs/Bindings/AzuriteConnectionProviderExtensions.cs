// <copyright file="AzuriteConnectionProviderExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for integrating AzuriteConnectionProvider with dependency injection.
/// </summary>
public static class AzuriteConnectionProviderExtensions
{
    /// <summary>
    /// Adds Azurite connection provider services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAzuriteConnectionProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Replace the configuration with enhanced version
        IConfiguration enhancedConfig = AzuriteConnectionProvider.CreateEnhancedConfiguration(configuration);
        services.AddSingleton(enhancedConfig);

        return services;
    }
}