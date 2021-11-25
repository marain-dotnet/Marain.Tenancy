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
    using Corvus.Tenancy;

    using global::Azure;
    using global::Azure.Storage.Blobs;
    using global::Azure.Storage.Blobs.Models;
    using global::Azure.Storage.Blobs.Specialized;

    using Newtonsoft.Json;

    internal class TenancyContainerSetupDirectToStorage : ITenancyContainerSetup
    {
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
            IEnumerable<KeyValuePair<string, object>>? properties)
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
            BlobContainerClient newTenantContainer = GetBlobContainerForTenant(newTenantId, serviceClient);
            await newTenantContainer.CreateIfNotExistsAsync().ConfigureAwait(false);
            BlockBlobClient tenantBlobInParentContainer = parentContainer.GetBlockBlobClient(@"live\" + newTenantId);

            // TODO: we actually want to be able to use the V2 config here
            IPropertyBag tenantProperties = this.propertyBagFactory.Create(PropertyBagValues.Build(values =>
                values
                    .Concat(properties ?? Enumerable.Empty<KeyValuePair<string, object>>())
                    .Append(new KeyValuePair<string, object>(
                        "StorageConfigurationV3__corvustenancy", this.configuration))));
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

        public async Task EnsureRootTenantContainerExistsAsync()
        {
            BlobServiceClient serviceClient = await this.GetBlobServiceClientAsync();
            await this.EnsureContainerForTenantExists(RootTenant.RootTenantId, serviceClient);
        }

        private static string HashAndEncodeBlobContainerName(string containerName)
        {
            byte[] byteContents = Encoding.UTF8.GetBytes(containerName);
            using SHA1CryptoServiceProvider hash = new ();
            byte[] hashedBytes = hash.ComputeHash(byteContents);
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

        private async Task<BlobContainerClient> EnsureContainerForTenantExists(
            string tenantId, BlobServiceClient serviceClient)
        {
            BlobContainerClient container = GetBlobContainerForTenant(tenantId, serviceClient);
            await container.CreateIfNotExistsAsync();
            return container;
        }
    }
}