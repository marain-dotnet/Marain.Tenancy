// <copyright file="AzureBlobStorageTenantStoreConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage
{
    using Corvus.Storage.Azure.BlobStorage;

    /// <summary>
    /// Configuration settings for <see cref="AzureBlobStorageTenantStore"/>.
    /// </summary>
    internal class AzureBlobStorageTenantStoreConfiguration
    {
        /// <summary>
        /// Creates a <see cref="AzureBlobStorageTenantStoreConfiguration"/>.
        /// </summary>
        /// <param name="rootStorageConfiguration">
        /// The <see cref="RootStorageConfiguration"/>.
        /// </param>
        /// <param name="propagateRootTenancyStorageConfigAsV2">
        /// The <see cref="PropagateRootTenancyStorageConfigAsV2"/> value.</param>
        public AzureBlobStorageTenantStoreConfiguration(
            BlobContainerConfiguration rootStorageConfiguration,
            bool propagateRootTenancyStorageConfigAsV2)
        {
            this.RootStorageConfiguration = rootStorageConfiguration;
            this.PropagateRootTenancyStorageConfigAsV2 = propagateRootTenancyStorageConfigAsV2;
        }

        /// <summary>
        /// Gets the storage configuration to use when accessing the root tenant.
        /// </summary>
        public BlobContainerConfiguration RootStorageConfiguration { get; }

        /// <summary>
        /// Gets a value indicating whether new child tenants of the root should get the tenancy
        /// storage configuration in V2 format, to support V2-V3 transition.
        /// </summary>
        public bool PropagateRootTenancyStorageConfigAsV2 { get; }
    }
}