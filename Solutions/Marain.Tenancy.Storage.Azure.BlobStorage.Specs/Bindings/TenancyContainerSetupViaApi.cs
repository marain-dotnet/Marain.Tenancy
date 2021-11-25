// <copyright file="TenancyContainerSetupViaApi.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

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
            IEnumerable<KeyValuePair<string, object>>? properties)
        {
            ITenant newTenant = await this.Store.CreateWellKnownChildTenantAsync(parentId, id, name);
            if (properties is not null)
            {
                newTenant = await this.Store.UpdateTenantAsync(newTenant.Id, propertiesToSetOrAdd: properties);
            }

            return newTenant;
        }

        public async Task<ITenant> EnsureChildTenantExistsAsync(string parentId, string name)
        {
            return await this.Store.CreateChildTenantAsync(parentId, name);
        }

        public Task EnsureRootTenantContainerExistsAsync()
        {
            throw new NotImplementedException();
        }
    }
}