// <copyright file="AzureBlobStorageTenantStore.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Corvus.Extensions;
    using Corvus.Extensions.Json;
    using Corvus.Json;
    using Corvus.Storage.Azure.BlobStorage;
    using Corvus.Storage.Azure.BlobStorage.Tenancy;
    using Corvus.Tenancy;
    using Corvus.Tenancy.Exceptions;

    using global::Azure;
    using global::Azure.Storage.Blobs;
    using global::Azure.Storage.Blobs.Models;
    using global::Azure.Storage.Blobs.Specialized;

    using Newtonsoft.Json;

    /// <summary>
    /// Tenant store implemented on Azure Blob Storage.
    /// </summary>
    internal class AzureBlobStorageTenantStore : ITenantStore
    {
        private const string TenancyContainerName = "corvustenancy";
        private const string TenancyV2ConfigKey = "StorageConfiguration__" + TenancyContainerName;
        private const string TenancyV3ConfigKey = "StorageConfigurationV3__" + TenancyContainerName;
        private const string LiveTenantsPrefix = "live/";
        private const string DeletedTenantsPrefix = "deleted/";
        private static readonly Encoding UTF8WithoutBom = new UTF8Encoding(false);
        private readonly IBlobContainerSourceWithTenantLegacyTransition containerSource;
        private readonly IPropertyBagFactory propertyBagFactory;
        private readonly JsonSerializer jsonSerializer;
        private readonly bool propagateRootStorageConfigAsV2;
        private Task? rootContainerExistsCheck;

        /// <summary>
        /// Creates a <see cref="AzureBlobStorageTenantStore"/>.
        /// </summary>
        /// <param name="configuration">Configuration settings.</param>
        /// <param name="containerSource">Provides access to tenanted blob storage.</param>
        /// <param name="serializerSettingsProvider">Settings for tenant serialization.</param>
        /// <param name="propertyBagFactory">Property bag services.</param>
        public AzureBlobStorageTenantStore(
            AzureBlobStorageTenantStoreConfiguration configuration,
            IBlobContainerSourceWithTenantLegacyTransition containerSource,
            IJsonSerializerSettingsProvider serializerSettingsProvider,
            IPropertyBagFactory propertyBagFactory)
        {
            this.containerSource = containerSource;
            this.propertyBagFactory = propertyBagFactory;
            this.jsonSerializer = JsonSerializer.Create(serializerSettingsProvider.Instance);
            this.propagateRootStorageConfigAsV2 = configuration.PropagateRootTenancyStorageConfigAsV2;

            // The root tenant is necessarily synthetic because we can't get access to storage
            // without it. And regardless of what stage of v2 to v3 transition we're in with
            // the store, we always configure the root tenant in v3 mode.
            this.Root = new RootTenant(propertyBagFactory);
            this.Root.UpdateProperties(values =>
                values.Append(new KeyValuePair<string, object>(
                    TenancyV3ConfigKey,
                    configuration.RootStorageConfiguration)));
        }

        /// <inheritdoc/>
        public RootTenant Root { get; }

        /// <inheritdoc/>
        public Task<ITenant> CreateChildTenantAsync(string parentTenantId, string name)
            => this.CreateWellKnownChildTenantAsync(parentTenantId, Guid.NewGuid(), name);

        /// <inheritdoc/>
        public async Task<ITenant> CreateWellKnownChildTenantAsync(
            string parentTenantId,
            Guid wellKnownChildTenantGuid,
            string name)
        {
            (ITenant parentTenant, BlobContainerClient container) =
                await this.GetContainerAndTenantForChildTenantsOfAsync(parentTenantId).ConfigureAwait(false);

            // We need to copy blob storage settings for the Tenancy container definition from the parent to the new child
            // to support the tenant blob store provider. We would expect this to be overridden by clients that wanted to
            // establish their own settings.
            bool configIsInV3 = true;
            LegacyV2BlobStorageConfiguration? v2TenancyStorageConfiguration = null;
            if (!parentTenant.Properties.TryGet(TenancyV3ConfigKey, out BlobContainerConfiguration tenancyStorageConfiguration))
            {
                configIsInV3 = false;
                if (!parentTenant.Properties.TryGet(TenancyV2ConfigKey, out v2TenancyStorageConfiguration))
                {
                    throw new InvalidOperationException($"No configuration found for ${TenancyV3ConfigKey} or ${TenancyV2ConfigKey}");
                }
            }

            IPropertyBag childProperties;
            if (parentTenantId == this.Root.Id && this.propagateRootStorageConfigAsV2)
            {
                configIsInV3 = false;
                v2TenancyStorageConfiguration = new LegacyV2BlobStorageConfiguration
                {
                    Container = tenancyStorageConfiguration.Container,
                };
                if (tenancyStorageConfiguration.ConnectionStringPlainText != null)
                {
                    v2TenancyStorageConfiguration.AccountName = tenancyStorageConfiguration.ConnectionStringPlainText;
                }
                else if (tenancyStorageConfiguration.AccountName != null)
                {
                    v2TenancyStorageConfiguration.AccountName = tenancyStorageConfiguration.AccountName;
                    v2TenancyStorageConfiguration.KeyVaultName = tenancyStorageConfiguration.AccessKeyInKeyVault?.VaultName;
                    v2TenancyStorageConfiguration.AccountKeySecretName = tenancyStorageConfiguration.AccessKeyInKeyVault?.SecretName;
                }
            }

            if (configIsInV3)
            {
                childProperties = this.propertyBagFactory.Create(values =>
                    values.Append(new KeyValuePair<string, object>(TenancyV3ConfigKey, tenancyStorageConfiguration)));
            }
            else
            {
                childProperties = this.propertyBagFactory.Create(values =>
                    values.Append(new KeyValuePair<string, object>(TenancyV2ConfigKey, v2TenancyStorageConfiguration!)));
            }

            var child = new Tenant(
                parentTenantId.CreateChildId(wellKnownChildTenantGuid),
                name,
                childProperties);

            // TODO: this needs thinking through.
            BlobContainerClient newTenantBlobContainer = await this.GetBlobContainer(child).ConfigureAwait(false);
            await newTenantBlobContainer.CreateIfNotExistsAsync().ConfigureAwait(false);

            // As we create the new blob, we need to ensure there isn't already a tenant with the same Id. We do this by
            // providing an If-None-Match header passing a "*", which will cause a storage exception with a 409 status
            // code if a blob with the same Id already exists.
            BlockBlobClient blob = GetLiveTenantBlockBlobReference(child.Id, container);
            var content = new MemoryStream();
            using (var sw = new StreamWriter(content, UTF8WithoutBom, leaveOpen: true))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                this.jsonSerializer.Serialize(writer, child);
            }

            content.Position = 0;
            try
            {
                Response<BlobContentInfo> response = await blob.UploadAsync(
                    content,
                    new BlobUploadOptions { Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All } })
                    .ConfigureAwait(false);
                child.ETag = response.Value.ETag.ToString("H");
            }
            catch (global::Azure.RequestFailedException x)
            when (x.ErrorCode == "BlobAlreadyExists")
            {
                // This exception is thrown because there's already a tenant with the same Id. This should never happen when
                // this method has been called from CreateChildTenantAsync as the Guid will have been generated and the
                // chances of it matching one previously generated are miniscule. However, it could happen when calling this
                // method directly with a wellKnownChildTenantGuid that's already in use. In this case, the fault is with
                // the client code - creating tenants with well known Ids is something one would expect to happen under
                // controlled conditions, so it's only likely that a conflict will occur when either the client code has made
                // a mistake or someone is actively trying to cause problems.
                throw new ArgumentException(
                    $"A child tenant of '{parentTenantId}' with a well known Guid of '{wellKnownChildTenantGuid}' already exists.",
                    nameof(wellKnownChildTenantGuid));
            }

            return child;
        }

        /// <inheritdoc/>
        public async Task DeleteTenantAsync(string tenantId)
        {
            string parentTenantId = TenantExtensions.GetRequiredParentId(tenantId);
            (_, BlobContainerClient parentContainer) =
                await this.GetContainerAndTenantForChildTenantsOfAsync(parentTenantId).ConfigureAwait(false);
            (_, BlobContainerClient tenantContainer) =
                await this.GetContainerAndTenantForChildTenantsOfAsync(tenantId).ConfigureAwait(false);

            // Check it's not empty first.
            AsyncPageable<BlobItem> pageable = tenantContainer.GetBlobsAsync(
                prefix: LiveTenantsPrefix);
            IAsyncEnumerable<Page<BlobItem>> pages = pageable.AsPages(pageSizeHint: 1);
            await using IAsyncEnumerator<Page<BlobItem>> page = pages.GetAsyncEnumerator();
            if (await page.MoveNextAsync() && page.Current.Values.Count > 0)
            {
                throw new ArgumentException($"Cannot delete tenant {tenantId} because it has children");
            }

            BlockBlobClient blob = GetLiveTenantBlockBlobReference(tenantId, parentContainer);
            Response<BlobDownloadResult> response = await blob.DownloadContentAsync().ConfigureAwait(false);
            using var blobContent = response.Value.Content.ToStream();
            BlockBlobClient? deletedBlob = parentContainer.GetBlockBlobClient(DeletedTenantsPrefix + tenantId);
            await deletedBlob.UploadAsync(blobContent).ConfigureAwait(false);
            await blob.DeleteIfExistsAsync().ConfigureAwait(false);

            await tenantContainer.DeleteAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<TenantCollectionResult> GetChildrenAsync(string tenantId, int limit, string? continuationToken)
        {
            (ITenant _, BlobContainerClient container) = await this.GetContainerAndTenantForChildTenantsOfAsync(tenantId)
                .ConfigureAwait(false);

            string? blobContinuationToken = DecodeUrlEncodedContinuationToken(continuationToken);

            AsyncPageable<BlobItem> pageable = container.GetBlobsAsync(prefix: LiveTenantsPrefix);
            IAsyncEnumerable<Page<BlobItem>> pages = pageable.AsPages(blobContinuationToken, limit);
            await using IAsyncEnumerator<Page<BlobItem>> page = pages.GetAsyncEnumerator();
            Page<BlobItem>? p = await page.MoveNextAsync()
                ? page.Current
                : null;
            IEnumerable<BlobItem> items = p?.Values ?? Enumerable.Empty<BlobItem>();

            return new TenantCollectionResult(
                items.Select(s => s.Name[LiveTenantsPrefix.Length..]).ToList(),
                GenerateContinuationToken(p?.ContinuationToken));
        }

        /// <inheritdoc/>
        public async Task<ITenant> GetTenantAsync(string tenantId, string? eTag = null)
        {
            if (tenantId == RootTenant.RootTenantId)
            {
                (ITenant result, _) = await this.GetContainerAndTenantForChildTenantsOfAsync(tenantId)
                    .ConfigureAwait(false);
                return result;
            }

            string parentTenantId;
            try
            {
                parentTenantId = TenantExtensions.GetRequiredParentId(tenantId);
            }
            catch (FormatException)
            {
                throw new TenantNotFoundException();
            }

            (ITenant _, BlobContainerClient container) =
                await this.GetContainerAndTenantForChildTenantsOfAsync(parentTenantId).ConfigureAwait(false);
            return await this.GetTenantFromContainerAsync(tenantId, container, eTag).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<ITenant> UpdateTenantAsync(
            string tenantId,
            string? name = null,
            IEnumerable<KeyValuePair<string, object>>? propertiesToSetOrAdd = null,
            IEnumerable<string>? propertiesToRemove = null)
        {
            string parentTenantId = TenantExtensions.GetRequiredParentId(tenantId);
            (ITenant _, BlobContainerClient parentContainer) =
                await this.GetContainerAndTenantForChildTenantsOfAsync(parentTenantId).ConfigureAwait(false);
            Tenant tenant = await this.GetTenantFromContainerAsync(tenantId, parentContainer, null).ConfigureAwait(false);
            string currentTenantEtag = tenant.ETag ?? throw new InvalidOperationException("Existing tenant does not have ETag");

            IPropertyBag updatedProperties = this.propertyBagFactory.CreateModified(
                tenant.Properties,
                propertiesToSetOrAdd,
                propertiesToRemove);

            var updatedTenant = new Tenant(
                tenant.Id,
                name ?? tenant.Name,
                updatedProperties);

            BlockBlobClient blob = GetLiveTenantBlockBlobReference(tenantId, parentContainer);
            var content = new MemoryStream();
            using (var sw = new StreamWriter(content, UTF8WithoutBom, leaveOpen: true))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                this.jsonSerializer.Serialize(writer, updatedTenant);
            }

            content.Position = 0;
            try
            {
                Response<BlobContentInfo> response = await blob.UploadAsync(
                    content,
                    new BlobUploadOptions { Conditions = new BlobRequestConditions { IfMatch = new ETag(currentTenantEtag) } })
                    .ConfigureAwait(false);
                updatedTenant.ETag = response.Value.ETag.ToString("H");
            }
            catch (RequestFailedException x)
            when (x.Status == (int)HttpStatusCode.PreconditionFailed)
            {
                // This indicates that the blob changed between us reading it and applying the modification.
                // TODO: perform a retry instead of bailing.
                throw new InvalidOperationException("Concurrent modifications detected.");
            }

            return updatedTenant;
        }

        private static BlockBlobClient GetLiveTenantBlockBlobReference(string tenantId, BlobContainerClient container)
        {
            return container.GetBlockBlobClient(LiveTenantsPrefix + tenantId);
        }

        /// <summary>
        /// Decodes a URL-friendly continuation token back to its original form.
        /// </summary>
        /// <param name="continuationTokenInUrlForm">The continuation token.</param>
        /// <returns>A <see cref="string"/> built from the continuation token.</returns>
        private static string? DecodeUrlEncodedContinuationToken(string? continuationTokenInUrlForm)
            => continuationTokenInUrlForm?.Base64UrlDecode();

        /// <summary>
        /// Generates a URL-friendly continuation token from a continuation token provided by blob storage.
        /// </summary>
        /// <param name="continuationToken">The continuation token in the form blob storage supplied it.</param>
        /// <returns>A URL-friendly string, or null if <paramref name="continuationToken"/> was null.</returns>
        private static string? GenerateContinuationToken(string? continuationToken)
            => string.IsNullOrWhiteSpace(continuationToken) ? null : continuationToken.Base64UrlEncode();

        private async Task<(ITenant ParentTenant, BlobContainerClient Container)> GetContainerAndTenantForChildTenantsOfAsync(string tenantId)
        {
            // Get the repo for the root tenant
            BlobContainerClient blobContainer = await this.GetBlobContainer(this.Root)
                .ConfigureAwait(false);

            if (tenantId == RootTenant.RootTenantId)
            {
                await LazyInitializer.EnsureInitialized(
                    ref this.rootContainerExistsCheck,
                    async () => await blobContainer.CreateIfNotExistsAsync().ConfigureAwait(false));
                return (this.Root, blobContainer);
            }

            ITenant currentTenant = this.Root;
            IEnumerable<string> ids = tenantId.GetParentTree();
            foreach (string id in ids)
            {
                // Get the tenant from its parent's container
                currentTenant = await this.GetTenantFromContainerAsync(id, blobContainer, null).ConfigureAwait(false);

                // Then get the container for that tenant
                blobContainer = await this.GetBlobContainer(currentTenant).ConfigureAwait(false);
            }

            // Finally, return the tenant repository which contains the children of the specified tenant
            return (currentTenant, blobContainer);
        }

        private async Task<BlobContainerClient> GetBlobContainer(ITenant tenant)
        {
            return await this.containerSource.GetBlobContainerClientFromTenantAsync(
                tenant,
                TenancyV2ConfigKey,
                TenancyV3ConfigKey,
                TenancyContainerName)
                .ConfigureAwait(false);
        }

        private async Task<Tenant> GetTenantFromContainerAsync(
            string tenantId, BlobContainerClient container, string? etag)
        {
            BlockBlobClient blob = GetLiveTenantBlockBlobReference(tenantId, container);
            Response<BlobDownloadResult> response;
            try
            {
                response = await blob.DownloadContentAsync(
                    string.IsNullOrEmpty(etag)
                        ? null
                        : new BlobRequestConditions { IfNoneMatch = new ETag(etag) },
                    CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (RequestFailedException x)
            when (x.Status == 404)
            {
                throw new TenantNotFoundException();
            }

            Response httpResponse = response.GetRawResponse();
            if (httpResponse.Status == (int)HttpStatusCode.NotModified)
            {
                throw new TenantNotModifiedException();
            }

            Tenant tenant;
            using (StreamReader sr = new(response.Value.Content.ToStream(), leaveOpen: false))
            using (JsonTextReader jr = new(sr))
            {
                tenant = this.jsonSerializer.Deserialize<Tenant>(jr)!;
            }

            tenant.ETag = response.Value.Details.ETag.ToString("H");
            return tenant;
        }
    }
}