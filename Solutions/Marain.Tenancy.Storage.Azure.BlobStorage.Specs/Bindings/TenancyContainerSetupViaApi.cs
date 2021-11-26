// <copyright file="TenancyContainerSetupViaApi.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Corvus.Storage.Azure.BlobStorage;
    using Corvus.Storage.Azure.BlobStorage.Tenancy;
    using Corvus.Tenancy;

    internal class TenancyContainerSetupViaApi : ITenancyContainerSetup
    {
        // Deferred tenant store fetching - it's important we don't try to use this before
        // we need it because it's not available until after the DI container has been built.
        private readonly Func<ITenantStore> getTenantStore;

        public TenancyContainerSetupViaApi(Func<ITenantStore> getTenantStore)
        {
            this.getTenantStore = getTenantStore;
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
                        AccountName = v3Config.ConnectionStringPlainText != null
                            ? v3Config.ConnectionStringPlainText
                            : v3Config.AccountName,
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
    }
}