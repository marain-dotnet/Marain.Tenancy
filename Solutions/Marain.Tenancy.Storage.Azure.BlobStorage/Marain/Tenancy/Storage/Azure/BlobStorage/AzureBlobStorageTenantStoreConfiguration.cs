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
        /// <param name="rootStorageConfiguration">The <see cref="RootStorageConfiguration"/>.</param>
        public AzureBlobStorageTenantStoreConfiguration(BlobContainerConfiguration rootStorageConfiguration)
        {
            this.RootStorageConfiguration = rootStorageConfiguration;
        }

        /// <summary>
        /// Gets the storage configuration to use when accessing the root tenant.
        /// </summary>
        public BlobContainerConfiguration RootStorageConfiguration { get; }
    }
}