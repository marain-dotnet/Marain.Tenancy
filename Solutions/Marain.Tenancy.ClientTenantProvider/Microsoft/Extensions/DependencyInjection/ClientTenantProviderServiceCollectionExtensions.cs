// <copyright file="ClientTenantProviderServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System.Collections.Generic;
    using System.Linq;
    using Corvus.ContentHandling;
    using Corvus.Extensions.Json;
    using Corvus.Json;
    using Corvus.Tenancy;
    using Marain.Tenancy;
    using Marain.Tenancy.Client;
    using Marain.Tenancy.Mappers;

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
                IPropertyBagFactory propertyBagFactory = s.GetRequiredService<IPropertyBagFactory>();
                ITenant fetchedRootTenant = tenantMapper.MapTenant(tenancyService.GetTenant(RootTenant.RootTenantId));
                var localRootTenant = new RootTenant(propertyBagFactory);
                IReadOnlyDictionary<string, object> propertiesToSetOrAdd = fetchedRootTenant.Properties.AsDictionary();
                localRootTenant.UpdateProperties(propertiesToSetOrAdd);
                return localRootTenant;
            });

            return services;
        }

        /// <summary>
        /// Adds services an Azure Blob storage-based implementation of <see cref="ITenantProvider"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The modified service collection.</returns>
        /// <remarks>
        /// Applications using this must also make <see cref="TenancyClientOptions"/> available via
        /// DI.
        /// </remarks>
        public static IServiceCollection AddTenantProviderServiceClient(
            this IServiceCollection services)
        {
            if (services.Any(s => typeof(ITenantProvider).IsAssignableFrom(s.ServiceType)))
            {
                return services;
            }

            services.AddTenancyClient();
            services.AddTenantServiceClientRootTenant();
            services.AddSingleton<ITenantMapper, TenantMapper>();
            services.AddSingleton<ITenantProvider, ClientTenantProvider>();
            services.AddSingleton<ITenantStore, ClientTenantStore>();
            return services;
        }
    }
}
