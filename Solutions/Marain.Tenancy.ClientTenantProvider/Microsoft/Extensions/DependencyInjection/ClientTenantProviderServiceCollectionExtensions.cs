// <copyright file="ClientTenantProviderServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Linq;
    using Corvus.ContentHandling;
    using Corvus.Extensions.Json;
    using Corvus.Tenancy;
    using Marain.Tenancy;
    using Marain.Tenancy.Client;
    using Marain.Tenancy.Mappers;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Extensions to register the Root tenant with the service collection.
    /// </summary>
    public static class ClientTenantProviderServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the root tenant to the collection, using the <see cref="ITenancyService"/>.
        /// </summary>
        /// <param name="services">The service collection to which to add the root tenant.</param>
        /// <returns>The configured service collection.</returns>
        public static IServiceCollection AddTenantServiceClientRootTenant(this IServiceCollection services)
        {
            if (services.Any(s => typeof(RootTenant).IsAssignableFrom(s.ServiceType)))
            {
                return services;
            }

            services.AddContent(contentFactory =>
            {
                contentFactory.RegisterTransientContent<Tenant>();
            });

            // Construct a root tenant from the tenant retrieved from the service, using the
            // root tenant ID.
            services.AddSingleton(s =>
            {
                ITenancyService tenancyService = s.GetRequiredService<ITenancyService>();
                ITenantMapper tenantMapper = s.GetRequiredService<ITenantMapper>();
                ITenant rootTenant = tenantMapper.MapTenant(tenancyService.GetTenant(RootTenant.RootTenantId));
                return new RootTenant(s.GetRequiredService<IJsonSerializerSettingsProvider>()) { ETag = rootTenant.ETag, Id = rootTenant.Id, Properties = rootTenant.Properties };
            });

            return services;
        }

        /// <summary>
        /// Adds services an Azure Blob storage-based implementation of <see cref="ITenantProvider"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="config">The configuration.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddTenantProviderServiceClient(
            this IServiceCollection services,
            IConfigurationRoot config)
        {
            if (services.Any(s => typeof(ITenantProvider).IsAssignableFrom(s.ServiceType)))
            {
                return services;
            }

            services.AddTenancyClient(new Uri(config["TenancyServiceBaseUri"]));
            services.AddTenantServiceClientRootTenant();
            services.AddSingleton<ITenantMapper, TenantMapper>();
            services.AddSingleton<ITenantProvider, ClientTenantProvider>();
            return services;
        }
    }
}
