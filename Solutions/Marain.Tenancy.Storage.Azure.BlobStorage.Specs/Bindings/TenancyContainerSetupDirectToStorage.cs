// <copyright file="TenancyContainerSetupDirectToStorage.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    using Corvus.Extensions.Json;
    using Corvus.Json;
    using Corvus.Storage.Azure.BlobStorage;
    using Corvus.Storage.Azure.BlobStorage.Tenancy;
    using Corvus.Tenancy;

    using global::Azure;
    using global::Azure.Storage.Blobs;
    using global::Azure.Storage.Blobs.Models;
    using global::Azure.Storage.Blobs.Specialized;

    using Newtonsoft.Json;

    using NUnit.Framework.Constraints;

    internal class TenancyContainerSetupDirectToStorage : ITenancyContainerSetup
    {
        private static readonly Lazy<SHA1> Sha1 = new (() => SHA1.Create());
        private readonly BlobContainerConfiguration configuration;
        private readonly IBlobContainerSourceByConfiguration blobContainerSource;
        private readonly IPropertyBagFactory propertyBagFactory;
        private readonly JsonSerializer jsonSerializer;

        public TenancyContainerSetupDirectToStorage(
            BlobContainerConfiguration configuration,
            IBlobContainerSourceByConfiguration blobContainerSource,
            IJsonSerializerSettingsProvider serializerSettingsProvider,
            IPropertyBagFactory propertyBagFactory)
        {
            this.configuration = configuration;
            this.blobContainerSource = blobContainerSource;
            this.propertyBagFactory = propertyBagFactory;

            this.jsonSerializer = JsonSerializer.Create(serializerSettingsProvider.Instance);
        }

        public async Task<ITenant> EnsureWellKnownChildTenantExistsAsync(
            string parentId,
            Guid id,
            string name,
            bool useV2Style,
            IEnumerable<KeyValuePair<string, object>>? properties = null)
        {
            BlobServiceClient serviceClient = await this.GetBlobServiceClientAsync().ConfigureAwait(false);

            // Note that we're not creating containers all the way down. This requires all
            // tenants from the root down to the parent of the new one to exist. We only
            // create the last one.
            // For test purposes, we set the root config into all child tenants instead of
            // propagating from the parent (because currently, no tests require anything more
            // complex than that).
            string newTenantId = parentId.CreateChildId(id);
            BlobContainerClient parentContainer = GetBlobContainerForTenant(parentId, serviceClient);
            await parentContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            BlobContainerClient newTenantContainer = GetBlobContainerForTenant(newTenantId, serviceClient);
            await newTenantContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            BlockBlobClient tenantBlobInParentContainer = parentContainer.GetBlockBlobClient(@"live\" + newTenantId);

            KeyValuePair<string, object> storageConfig = useV2Style
                ? new KeyValuePair<string, object>(
                    "StorageConfiguration__corvustenancy",
                    new LegacyV2BlobStorageConfiguration
                    {
                        AccountName = this.configuration.ConnectionStringPlainText ?? this.configuration.AccountName,
                        KeyVaultName = this.configuration.AccessKeyInKeyVault?.VaultName,
                        AccountKeySecretName = this.configuration.AccessKeyInKeyVault?.SecretName,
                    })
                : new KeyValuePair<string, object>("StorageConfigurationV3__corvustenancy", this.configuration);

            IPropertyBag tenantProperties = this.propertyBagFactory.Create(PropertyBagValues.Build(values =>
                values
                    .Concat(properties ?? Enumerable.Empty<KeyValuePair<string, object>>())
                    .Append(storageConfig)));
            Tenant newTenant = new (newTenantId, name, tenantProperties);
            var content = new MemoryStream();
            using (var sw = new StreamWriter(content, new UTF8Encoding(false), leaveOpen: true))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                this.jsonSerializer.Serialize(writer, newTenant);
            }

            content.Position = 0;
            Response<BlobContentInfo> blobUploadResponse = await tenantBlobInParentContainer.UploadAsync(content).ConfigureAwait(false);
            newTenant.ETag = blobUploadResponse.Value.ETag.ToString("H");

            return newTenant;
        }

        public async Task DeleteTenantAsync(string tenantId, bool leaveContainer)
        {
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
                this.configuration.ForContainer("dummy"));
            return rootTenantContainerFromConfig.GetParentBlobServiceClient();
        }
    }
}