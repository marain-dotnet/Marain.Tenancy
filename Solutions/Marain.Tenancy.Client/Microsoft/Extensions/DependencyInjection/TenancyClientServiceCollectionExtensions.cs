// <copyright file="TenancyClientServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection;

using System;
using System.Net.Http;
using CacheCow.Client;
using Corvus.Identity.ClientAuthentication;
using Corvus.Json.Serialization;
using Marain.Clients;
using Marain.Tenancy;
using Marain.Tenancy.Client;

/// <summary>
/// DI initialization for clients of the Tenancy service.
/// </summary>
public static class TenancyClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Tenancy client to a service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configurationCallback">A callback function that will retrieve the client configuration.</param>
    /// <param name="enableResponseCaching">If true, responses will be cached based on the Cache-Control headers sent from the server.</param>
    /// <param name="messageHandler">An optional default message handler for the underlying HttpClient. Primarily used for testing scenarios.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddTenancyClient(
        this IServiceCollection services,
        Func<IServiceProvider, TenancyApiClientConfiguration> configurationCallback,
        bool enableResponseCaching = true,
        HttpMessageHandler? messageHandler = null)
    {
        IHttpClientBuilder httpClientBuilder = services.AddHttpClient(nameof(TenancyClient)).ConfigureHttpClient((sp, client) =>
        {
            TenancyApiClientConfiguration configuration = configurationCallback(sp);
            client.BaseAddress = new Uri(configuration.BaseUri);
        });

        if (messageHandler is not null)
        {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => messageHandler);
        }

        httpClientBuilder.ConfigureAdditionalHttpMessageHandlers((handlers, sp) =>
        {
            TenancyApiClientConfiguration configuration = configurationCallback(sp);
            if (!string.IsNullOrEmpty(configuration.ResourceIdForMsiAuthentication))
            {
                IServiceIdentityAccessTokenSource tokenSource = sp.GetRequiredService<IServiceIdentityAccessTokenSource>();
                var authHandler = new TokenCredentialHandler(tokenSource, configuration.ResourceIdForMsiAuthentication);
                handlers.Add(authHandler);
            }

            if (enableResponseCaching)
            {
                handlers.Add(new CachingHandler());
            }
        });

        httpClientBuilder.AddStandardResilienceHandler();

        services.AddSingleton<ITenancyClient>(
            sp =>
            {
                IHttpClientFactory httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                IJsonSerializerOptionsProvider serializerOptionsProvider = sp.GetRequiredService<IJsonSerializerOptionsProvider>();

                HttpClient client = httpClientFactory.CreateClient(nameof(TenancyClient));
                return new TenancyClient(client, serializerOptionsProvider.Instance);
            });

        return services;
    }
}