// <copyright file="TenancyClientServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection;

using System.Net.Http;
using Azure.Core;

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
        TokenCredential? tokenCredential = null,
        HttpMessageHandler? messageHandler = null)
    {
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
        string baseUrl,
        HttpMessageHandler? messageHandler = null) => services.AddTenancyClient(baseUrl, null, messageHandler);
}