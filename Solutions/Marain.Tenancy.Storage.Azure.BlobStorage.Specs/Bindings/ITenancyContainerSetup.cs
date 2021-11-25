// <copyright file="ITenancyContainerSetup.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Corvus.Tenancy;

    internal interface ITenancyContainerSetup
    {
        Task EnsureRootTenantContainerExistsAsync();

        Task<ITenant> EnsureWellKnownChildTenantExistsAsync(
            string parentId,
            Guid id,
            string name,
            IEnumerable<KeyValuePair<string, object>>? properties = null);

        Task<ITenant> EnsureChildTenantExistsAsync(
            string parentId,
            string name,
            IEnumerable<KeyValuePair<string, object>>? properties = null)
            => this.EnsureWellKnownChildTenantExistsAsync(parentId, Guid.NewGuid(), name, properties);
    }
}