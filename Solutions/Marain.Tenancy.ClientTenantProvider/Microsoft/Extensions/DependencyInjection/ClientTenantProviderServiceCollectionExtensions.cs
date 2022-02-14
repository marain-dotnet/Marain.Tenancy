// <copyright file="ClientTenantProviderServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System.Collections.Generic;
    using System.Linq;
    using Corvus.ContentHandling;
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

            services.AddContent(contentFactory => contentFactory.RegisterTransientContent<Tenant>());

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
        /// <param name="enableResponseCaching">Flag indicating whether or not response caching should be enabled for
        /// GET operations.</param>
        /// <returns>The modified service collection.</returns>
        /// <remarks>
        /// <para>
        /// Applications using this must also make <see cref="TenancyClientOptions"/> available via DI.
        /// </para>
        /// <para>
        /// If you are intending to use <see cref="ITenantProvider" /> to retrieve tenants, you are advised to set
        /// <c>enableResponseCaching</c> to true. However, if you are planning to create or update tenants using
        /// <see cref="ITenantStore" /> to create and update tenants, you should set it to false to ensure that you
        /// always retrieve the latest version of a tenant.
        /// </para>
        /// </remarks>
        public static IServiceCollection AddTenantProviderServiceClient(
            this IServiceCollection services,
            bool enableResponseCaching = true)
        {
            if (services.Any(s => typeof(ITenantProvider).IsAssignableFrom(s.ServiceType)))
            {
                return services;
            }

            services.AddTenancyClient(enableResponseCaching);
            services.AddTenantServiceClientRootTenant();
            services.AddSingleton<ITenantMapper, TenantMapper>();
            services.AddSingleton<ITenantProvider, ClientTenantProvider>();
            services.AddSingleton<ITenantStore, ClientTenantStore>();
            return services;
        }
    }
}