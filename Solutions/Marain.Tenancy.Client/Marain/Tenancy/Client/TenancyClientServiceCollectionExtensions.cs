// <copyright file="TenancyClientServiceCollectionExtensions.cs" company="Endjin">
// Copyright (c) Endjin. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using CacheCow.Client;
    using Corvus.Extensions.Json;
    using Corvus.Identity.ManagedServiceIdentity.ClientAuthentication;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Rest;
    using Newtonsoft.Json;

    /// <summary>
    /// DI initialization for clients of the Tenancy service.
    /// </summary>
    public static class TenancyClientServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Tenancy client to a service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The modified service collection.</returns>
        /// <remarks>
        /// This requires the <see cref="TenancyClientOptions"/> to be available from DI in order
        /// to discover the base URI of the Operations control service, and, if required, to
        /// specify the resource id to use when obtaining an authentication token representing the
        /// hosting service's identity.
        /// </remarks>
        [Obsolete("Prefer AddTenancyClient(IServiceCollection, bool) to explicitly state whether response caching should be enabled.")]
        public static IServiceCollection AddTenancyClient(
            this IServiceCollection services)
        {
            return services.AddTenancyClient(false);
        }

        /// <summary>
        /// Adds the Tenancy client to a service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="enableResponseCaching">Flag indicating whether or not response caching should be enabled for GET operations.</param>
        /// <returns>The modified service collection.</returns>
        /// <remarks>
        /// This requires the <see cref="TenancyClientOptions"/> to be available from DI in order
        /// to discover the base URI of the Operations control service, and, if required, to
        /// specify the resource id to use when obtaining an authentication token representing the
        /// hosting service's identity.
        /// </remarks>
        public static IServiceCollection AddTenancyClient(
            this IServiceCollection services,
            bool enableResponseCaching)
        {
            if (services.Any(s => s.ServiceType == typeof(ITenancyService)))
            {
                return services;
            }

            services.AddContentTypeBasedSerializationSupport();
            services.AddSingleton((Func<IServiceProvider, ITenancyService>)(sp =>
            {
                TenancyClientOptions options = sp.GetRequiredService<TenancyClientOptions>();

                DelegatingHandler[] handlers = enableResponseCaching
                    ? new DelegatingHandler[] { new CachingHandler() }
                    : Array.Empty<DelegatingHandler>();

                TenancyService service;
                if (string.IsNullOrWhiteSpace(options.ResourceIdForMsiAuthentication))
                {
                    service = new UnauthenticatedTenancyService(options.TenancyServiceBaseUri, handlers);
                }
                else
                {
                    var tokenCredentials = new TokenCredentials(
                        new ServiceIdentityTokenProvider(
                            sp.GetRequiredService<IServiceIdentityTokenSource>(),
                            options.ResourceIdForMsiAuthentication));
                    service = new TenancyService(options.TenancyServiceBaseUri, tokenCredentials, handlers);
                }

                JsonSerializerSettings serializerSettings = sp.GetRequiredService<IJsonSerializerSettingsProvider>().Instance;
                serializerSettings.Converters.ForEach(service.SerializationSettings.Converters.Add);
                serializerSettings.Converters.ForEach(service.DeserializationSettings.Converters.Add);

                service.DeserializationSettings.DateParseHandling = serializerSettings.DateParseHandling;

                return service;
            }));

            return services;
        }
    }
}
