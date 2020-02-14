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
        /// <returns>The modified service collection.</returns>
        /// <remarks>
        /// This requires the <see cref="TenancyClientOptions"/> to be available from DI in order
        /// to discover the base URI of the Operations control service, and, if required, to
        /// specify the resource id to use when obtaining an authentication token representing the
        /// hosting service's identity.
        /// </remarks>
        public static IServiceCollection AddTenancyClient(
            this IServiceCollection services)
        {
            if (services.Any(s => s.ServiceType == typeof(ITenancyService)))
            {
                return services;
            }

            services.AddContentSerialization();
            services.AddSingleton<ITenancyService>(sp =>
            {
                TenancyClientOptions options = sp.GetRequiredService<TenancyClientOptions>();

                if (string.IsNullOrWhiteSpace(options.ResourceIdForMsiAuthentication))
                {
                    return new UnauthenticatedTenancyService(sp.GetRequiredService<TenancyClientOptions>().TenancyServiceBaseUri);
                }
                else
                {
                    var service = new TenancyService(options.TenancyServiceBaseUri, new TokenCredentials(
                        new ServiceIdentityTokenProvider(
                            sp.GetRequiredService<IServiceIdentityTokenSource>(),
                            options.ResourceIdForMsiAuthentication)));

                    sp.GetRequiredService<IJsonSerializerSettingsProvider>().Instance.Converters.ForEach(service.SerializationSettings.Converters.Add);
                    sp.GetRequiredService<IJsonSerializerSettingsProvider>().Instance.Converters.ForEach(service.DeserializationSettings.Converters.Add);

                    return service;
                }
            });

            return services;
        }
    }
}
