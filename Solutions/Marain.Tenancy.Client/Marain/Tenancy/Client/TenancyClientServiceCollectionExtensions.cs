// <copyright file="TenancyClientServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client;

using System;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.Kiota.Http.HttpClientLibrary;

/// <summary>
/// DI initialization for clients of the Tenancy service.
/// </summary>
public static class TenancyClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Tenancy client to a service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">The base URL for the Tenancy API.</param>
    /// <param name="tokenCredential">Optional token credential for authentication.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddTenancyClient(
        this IServiceCollection services,
        string baseUrl,
        TokenCredential? tokenCredential = null)
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
            if (tokenCredential != null)
            {
                return new AzureIdentityAuthenticationProvider(tokenCredential);
            }
            else
            {
                return new AnonymousAuthenticationProvider();
            }
        });

        // Register request adapter
        services.AddSingleton(serviceProvider =>
        {
            IAuthenticationProvider authProvider = serviceProvider.GetRequiredService<IAuthenticationProvider>();
            HttpClientRequestAdapter adapter = new(authProvider);
            adapter.BaseUrl = baseUrl;
            return adapter;
        });

        // Register the Kiota client and service wrapper
        services.AddSingleton<TenancyApiClient>();
        services.AddSingleton<ITenancyService, TenancyService>();

        return services;
    }

    /// <summary>
    /// Adds the Tenancy client to a service collection with unauthenticated access.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">The base URL for the Tenancy API.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddUnauthenticatedTenancyClient(
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

        // Register the Kiota client and service wrapper
        services.AddSingleton<TenancyApiClient>();
        services.AddSingleton<ITenancyService, TenancyService>();

        return services;
    }
}