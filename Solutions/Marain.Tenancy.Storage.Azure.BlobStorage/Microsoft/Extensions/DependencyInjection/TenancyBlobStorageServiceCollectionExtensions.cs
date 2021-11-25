// <copyright file="TenancyBlobStorageServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using Corvus.Storage.Azure.BlobStorage;
    using Corvus.Tenancy;
    using Corvus.Tenancy.Internal;

    using Marain.Tenancy.Storage.Azure.BlobStorage;

    /// <summary>
    /// Configuration of services for using the operations repository implemented on top of the
    /// tenancy repository.
    /// </summary>
    public static class TenancyBlobStorageServiceCollectionExtensions
    {
        /// <summary>
        /// Enable the tenancy st.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="rootStorageConfiguration">
        /// Storage configuration to use when accessing the root tenant.
        /// </param>
        /// <returns>The modified service collection.</returns>
        public static IServiceCollection AddTenantStoreOnAzureBlobStorage(
            this IServiceCollection services,
            BlobContainerConfiguration rootStorageConfiguration)
        {
            services.AddAzureBlobStorageClient();

            // TODO: this next one is temporary.
            // Corvus.Storage.Azure.BlobStorage.Tenancy should offer some successor to
            // AddTenantCloudBlobContainerFactory, but since it currently only offers the
            // transitional API (there is not yet an implementation of a native-v3 API)
            // that hasn't been written yet, and so we need to call this bootstrapper
            // in the Corvus.Tenancy.Internal namespace.
            services.AddRequiredTenancyServices();

            // TODO: This probably should also be called by Corvus.Storage.Azure.BlobStorage.Tenancy
            services.AddAzureBlobStorageClient();

            services.AddBlobContainerV2ToV3Transition();
            services.AddSingleton(new AzureBlobStorageTenantStoreConfiguration(rootStorageConfiguration));
            services.AddSingleton<ITenantStore, AzureBlobStorageTenantStore>();
            return services;
        }
    }
}