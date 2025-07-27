// <copyright file="TenancyKiotaServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.KiotaClient;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.Kiota.Http.HttpClientLibrary;

/// <summary>
/// DI initialization for Kiota-based clients of the Tenancy service.
/// </summary>
public static class TenancyKiotaServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Kiota-based Tenancy client to a service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">The base URL for the Tenancy API.</param>
    /// <param name="configureAuth">Optional configuration for authentication provider.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddTenancyKiotaClient(
        this IServiceCollection services,
        string baseUrl,
        Action<AzureIdentityAuthenticationProviderOptions>? configureAuth = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentNullException(nameof(baseUrl));
        }

        // Register HTTP client for Kiota
        services.AddHttpClient();

        // Register authentication provider
        services.AddSingleton<IAuthenticationProvider>(serviceProvider =>
        {
            AzureIdentityAuthenticationProviderOptions options = new();
            configureAuth?.Invoke(options);
            return new AzureIdentityAuthenticationProvider(options);
        });

        // Register request adapter
        services.AddSingleton(serviceProvider =>
        {
            IAuthenticationProvider authProvider = serviceProvider.GetRequiredService<IAuthenticationProvider>();
            HttpClientRequestAdapter adapter = new(authProvider);
            adapter.BaseUrl = baseUrl;
            return adapter;
        });

        // Register the Kiota client
        services.AddSingleton<TenancyApiClient>();

        return services;
    }

    /// <summary>
    /// Adds the Kiota-based Tenancy client to a service collection with unauthenticated access.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">The base URL for the Tenancy API.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddUnauthenticatedTenancyKiotaClient(
        this IServiceCollection services,
        string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentNullException(nameof(baseUrl));
        }

        // Register HTTP client for Kiota
        services.AddHttpClient();

        // Register anonymous authentication provider
        services.AddSingleton<IAuthenticationProvider, AnonymousAuthenticationProvider>();

        // Register request adapter
        services.AddSingleton(serviceProvider =>
        {
            IAuthenticationProvider authProvider = serviceProvider.GetRequiredService<IAuthenticationProvider>();
            HttpClientRequestAdapter adapter = new(authProvider);
            adapter.BaseUrl = baseUrl;
            return adapter;
        });

        // Register the Kiota client
        services.AddSingleton<TenancyApiClient>();

        return services;
    }
}