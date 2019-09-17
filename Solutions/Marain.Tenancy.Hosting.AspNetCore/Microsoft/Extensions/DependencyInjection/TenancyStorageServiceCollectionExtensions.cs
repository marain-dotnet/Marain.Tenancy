// <copyright file="TenancyStorageServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using Marain.Tenancy.OpenApi;
    using Menes;
    using Microsoft.Extensions.Configuration;

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
        /// <param name="rootTenantDefaultConfiguration">
        /// Configuration section to read root tenant default repository settings from.
        /// </param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddTenancyBlobContainer(
            this IServiceCollection services,
            IConfiguration rootTenantDefaultConfiguration)
        {
            services.AddTenantProviderBlobStore();
            services.AddTenantCloudBlobContainerFactory(rootTenantDefaultConfiguration);
            return services;
        }

        /// <summary>
        /// Add the temnancy api.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="rootTenantDefaultConfiguration">
        /// Configuration section to read root tenant default repository settings from.
        /// </param>
        /// <param name="configureHost">The optional action to configure the host.</param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddTenancyApi(
            this IServiceCollection services,
            IConfiguration rootTenantDefaultConfiguration,
            Action<IOpenApiHostConfiguration> configureHost = null)
        {
            services.AddTenancyBlobContainer(rootTenantDefaultConfiguration);
            services.AddTenancyApi(configureHost);
            return services;
        }
    }
}
