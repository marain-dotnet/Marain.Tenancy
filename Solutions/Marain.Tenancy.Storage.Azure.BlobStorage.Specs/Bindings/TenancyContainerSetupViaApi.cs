// <copyright file="TenancyContainerSetupViaApi.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    using Corvus.Storage.Azure.BlobStorage;
    using Corvus.Storage.Azure.BlobStorage.Tenancy;
    using Corvus.Tenancy;

    using global::Azure.Storage.Blobs;
    using global::Azure.Storage.Blobs.Specialized;

    internal class TenancyContainerSetupViaApi : ITenancyContainerSetup
    {
        private static readonly Lazy<SHA1> Sha1 = new (() => SHA1.Create());
        private readonly BlobContainerConfiguration configuration;

        // Deferred tenant store fetching - it's important we don't try to use this before
        // we need it because it's not available until after the DI container has been built.
        private readonly Func<ITenantStore> getTenantStore;
        private readonly IBlobContainerSourceFromDynamicConfiguration blobContainerSource;

        public TenancyContainerSetupViaApi(
            BlobContainerConfiguration configuration,
            Func<ITenantStore> getTenantStore,
            IBlobContainerSourceFromDynamicConfiguration blobContainerSource)
        {
            this.configuration = configuration;
            this.getTenantStore = getTenantStore;
            this.blobContainerSource = blobContainerSource;
        }

        private ITenantStore Store => this.getTenantStore();

        public async Task<ITenant> EnsureWellKnownChildTenantExistsAsync(
            string parentId,
            Guid id,
            string name,
            bool useV2Style,
            IEnumerable<KeyValuePair<string, object>>? properties = null)
        {
            ITenant newTenant = await this.Store.CreateWellKnownChildTenantAsync(parentId, id, name);

            if (properties is not null)
            {
                newTenant = await this.Store.UpdateTenantAsync(newTenant.Id, propertiesToSetOrAdd: properties);
            }

            KeyValuePair<string, object>? storageConfig = null;
            if (useV2Style)
            {
                if (newTenant.Properties.TryGet("StorageConfigurationV3__corvustenancy", out BlobContainerConfiguration v3Config))
                {
                    var v2Config = new LegacyV2BlobStorageConfiguration
                    {
                        AccountName = v3Config.ConnectionStringPlainText ?? v3Config.AccountName,
                        KeyVaultName = v3Config.AccessKeyInKeyVault?.VaultName,
                        AccountKeySecretName = v3Config.AccessKeyInKeyVault?.SecretName,
                    };

                    storageConfig = new ("StorageConfiguration__corvustenancy", v2Config);
                }
            }
            else
            {
                if (newTenant.Properties.TryGet("StorageConfigurationV__corvustenancy", out LegacyV2BlobStorageConfiguration v2Config))
                {
                    BlobContainerConfiguration v3Config = LegacyConfigurationConverter.FromV2ToV3(v2Config);

                    storageConfig = new ("StorageConfigurationV3__corvustenancy", v3Config);
                }
            }

            if (storageConfig is KeyValuePair<string, object> nnsc)
            {
                newTenant = await this.Store.UpdateTenantAsync(
                    newTenant.Id,
                    propertiesToSetOrAdd: new[] { nnsc });
            }

            return newTenant;
        }

        public async Task<ITenant> EnsureChildTenantExistsAsync(string parentId, string name)
        {
            return await this.Store.CreateChildTenantAsync(parentId, name);
        }

        public async Task DeleteTenantAsync(string tenantId, bool leaveContainer)
        {
            // We talk directly to the storage service here because for well-known tenants
            // we need to leave container in place to avoid tests failure with
            // Status: 409 (The specified container is being deleted. Try operation later.)
            // when we try to create a container with the same name as one we just deleted.
            BlobServiceClient serviceClient = await this.GetBlobServiceClientAsync().ConfigureAwait(false);

            string parentId = TenantExtensions.GetRequiredParentId(tenantId);
            BlobContainerClient parentContainer = GetBlobContainerForTenant(parentId, serviceClient);
            BlobContainerClient tenantContainer = GetBlobContainerForTenant(tenantId, serviceClient);
            BlockBlobClient tenantBlobInParentContainer = parentContainer.GetBlockBlobClient(@"live\" + tenantId);
            await tenantBlobInParentContainer.DeleteAsync().ConfigureAwait(false);

            if (!leaveContainer)
            {
                await tenantContainer.DeleteAsync().ConfigureAwait(false);
            }
        }

        private static string HashAndEncodeBlobContainerName(string containerName)
        {
            byte[] byteContents = Encoding.UTF8.GetBytes(containerName);
            byte[] hashedBytes = Sha1.Value.ComputeHash(byteContents);
            return TenantExtensions.ByteArrayToHexViaLookup32(hashedBytes);
        }

        private static BlobContainerClient GetBlobContainerForTenant(string tenantId, BlobServiceClient serviceClient)
        {
            string tenantedContainerName = $"{tenantId.ToLowerInvariant()}-corvustenancy";
            string containerName = HashAndEncodeBlobContainerName(tenantedContainerName);

            BlobContainerClient container = serviceClient.GetBlobContainerClient(containerName);
            return container;
        }

        private async Task<BlobServiceClient> GetBlobServiceClientAsync()
        {
            // We're using Corvus.Storage to process the configuration, but all we actually need
            // from it is a BlobServiceClient, because for this test mode, we want to work
            // directly with the storage API to validate that it works when the storage account
            // wasn't previously populated by the current Corvus and Marain APIs.
            BlobContainerClient rootTenantContainerFromConfig = await this.blobContainerSource.GetStorageContextAsync(
#pragma warning disable SA1101 // Prefix local calls with this - StyleCop doesn't recognize records, or the with syntax yet
                this.configuration with { Container = "dummy" });
#pragma warning restore SA1101 // Prefix local calls with this
            return rootTenantContainerFromConfig.GetParentBlobServiceClient();
        }
    }
}