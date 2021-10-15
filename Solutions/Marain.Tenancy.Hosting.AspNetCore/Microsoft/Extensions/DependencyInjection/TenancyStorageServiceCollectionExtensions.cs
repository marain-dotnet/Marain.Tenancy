// <copyright file="TenancyStorageServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using Corvus.Azure.Storage.Tenancy;
    using Menes;

    /// <summary>
    /// Configuration of services for using the operations repository implemented on top of the
    /// tenancy repository.
    /// </summary>
    public static class TenancyStorageServiceCollectionExtensions
    {
        /// <summary>
        /// Enable the tenancy st.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getRootTenantStorageConfiguration">
        /// A function that will retrieve storage configuration for the root tenant.
        /// </param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddTenancyBlobContainer(
            this IServiceCollection services,
            Func<IServiceProvider, BlobStorageConfiguration> getRootTenantStorageConfiguration)
        {
            services.AddTenantProviderBlobStore(getRootTenantStorageConfiguration);
            services.AddTenantCloudBlobContainerFactory(sp => sp.GetRequiredService<TenantCloudBlobContainerFactoryOptions>());
            return services;
        }

        /// <summary>
        /// Add the temnancy api.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="getRootTenantStorageConfiguration">
        /// A function that will retrieve storage configuration for the root tenant.
        /// </param>
        /// <param name="configureHost">The optional action to configure the host.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddTenancyApiOnBlobStorage(
            this IServiceCollection services,
            Func<IServiceProvider, BlobStorageConfiguration> getRootTenantStorageConfiguration,
            Action<IOpenApiHostConfiguration> configureHost = null)
        {
            services.AddTenancyBlobContainer(getRootTenantStorageConfiguration);
            services.AddTenancyApi();
            return services;
        }
    }
}
