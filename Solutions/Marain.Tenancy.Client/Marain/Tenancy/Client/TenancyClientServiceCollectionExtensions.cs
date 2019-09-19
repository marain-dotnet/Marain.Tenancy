// <copyright file="TenancyClientServiceCollectionExtensions.cs" company="Endjin">
// Copyright (c) Endjin. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client
{
    using System;
    using System.Linq;
    using Corvus.Extensions.Json;
    using Corvus.Identity.ManagedServiceIdentity.ClientAuthentication;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Rest;

    /// <summary>
    /// DI initialization for clients of the Tenancy service.
    /// </summary>
    public static class TenancyClientServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the Tenancy client to a service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="baseUri">The base URI of the Operations control service.</param>
        /// <param name="resourceIdForMsiAuthentication">
        /// The resource id to use when obtaining an authentication token representing the
        /// hosting service's identity. Pass null to run without authentication.
        /// </param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddTenancyClient(
            this IServiceCollection services,
            Uri baseUri,
            string resourceIdForMsiAuthentication = null)
        {
            if (services.Any(s => s.ServiceType == typeof(ITenancyService)))
            {
                return services;
            }

            services.AddContentSerialization();

            return resourceIdForMsiAuthentication == null
                ? services.AddSingleton<ITenancyService>(new UnauthenticatedTenancyService(baseUri))
                : services.AddSingleton<ITenancyService>(sp =>
                {
                    var service = new TenancyService(baseUri, new TokenCredentials(
                        new ServiceIdentityTokenProvider(
                            sp.GetRequiredService<IServiceIdentityTokenSource>(),
                            resourceIdForMsiAuthentication)));

                    sp.GetRequiredService<IJsonSerializerSettingsProvider>().Instance.Converters.ForEach(service.SerializationSettings.Converters.Add);
                    sp.GetRequiredService<IJsonSerializerSettingsProvider>().Instance.Converters.ForEach(service.DeserializationSettings.Converters.Add);

                    return service;
                });
        }
    }
}
