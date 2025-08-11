// <copyright file="TenancyClientServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection;

using System;
using System.Net.Http;
using Azure.Core;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Serialization;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Authentication.Azure;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

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
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentNullException(nameof(baseUrl));
        }

        // Register authentication provider
        services.AddSingleton<IAuthenticationProvider>(serviceProvider =>
        {
            if (tokenCredential is null)
            {
                return new AnonymousAuthenticationProvider();
            }
            else
            {
                return new AzureIdentityAuthenticationProvider(tokenCredential);
            }
        });

        // Register request adapter
        services.AddSingleton<IRequestAdapter>(serviceProvider =>
        {
            IAuthenticationProvider authProvider = serviceProvider.GetRequiredService<IAuthenticationProvider>();

            ////var headersInspectionHandler = new HeadersInspectionHandler(new HeadersInspectionHandlerOption { InspectResponseHeaders = true });
            DelegatingHandler[] allHandlers = [
                ..KiotaClientFactory.CreateDefaultHandlers(),
                ////headersInspectionHandler,
            ];

            DelegatingHandler chainedHandlers = KiotaClientFactory.ChainHandlersCollectionAndGetFirstLink(
                messageHandler ?? KiotaClientFactory.GetDefaultHttpMessageHandler(),
                allHandlers) ?? throw new InvalidOperationException("Unable to create Http pipeline handlers");

            HttpClient client = new(chainedHandlers);

            HttpClientRequestAdapter adapter = new(authProvider, httpClient: client)
            {
                BaseUrl = baseUrl
            };

            return adapter;
        });

        services.AddHttpClient();

        services.AddJsonSerializerOptionsProvider();
        services.AddJsonCultureInfoConverter();
        services.AddJsonDateTimeOffsetToIso8601AndUnixTimeConverter();
        services.AddCamelCaseConverterForEnums();

        // Register custom serialization factories
        services.AddSingleton<CorvusJsonSerializationWriterFactory>();
        services.AddSingleton<CorvusJsonParseNodeFactory>();

        services.AddSingleton<TenancyApiClient>();
        services.AddSingleton(serviceProvider =>
        {
            IRequestAdapter requestAdapter = serviceProvider.GetRequiredService<IRequestAdapter>();
            var client = new TenancyApiClient(requestAdapter);

            CorvusJsonSerializationWriterFactory serializerFactory = serviceProvider.GetRequiredService<CorvusJsonSerializationWriterFactory>();
            CorvusJsonParseNodeFactory parseNodeFactory = serviceProvider.GetRequiredService<CorvusJsonParseNodeFactory>();

            // Register custom factories globally with Kiota's registries
            SerializationWriterFactoryRegistry.DefaultInstance.ContentTypeAssociatedFactories[serializerFactory.ValidContentType] = serializerFactory;
            ParseNodeFactoryRegistry.DefaultInstance.ContentTypeAssociatedFactories[parseNodeFactory.ValidContentType] = parseNodeFactory;

            return client;
        });

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
        HttpMessageHandler? messageHandler = null) => AddTenancyClient(services, baseUrl, null, messageHandler);

    /// <summary>
    /// Adds the Tenancy client to a service collection with unauthenticated access and Corvus serialization enabled.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">The base URL for the Tenancy API.</param>
    /// <param name="messageHandler">Optional custom message handler.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddUnauthenticatedTenancyClientWithCorvusSerialization(
        this IServiceCollection services,
        string baseUrl,
        HttpMessageHandler? messageHandler = null)
    {
        // Add base client services first
        services.AddTenancyClient(baseUrl, null, messageHandler);
        
        // Register custom serialization factories
        services.AddSingleton<CorvusJsonSerializationWriterFactory>();
        services.AddSingleton<CorvusJsonParseNodeFactory>();
        
        // The Corvus serialization factories are automatically registered when the TenancyApiClient is created
        
        return services;
    }
}