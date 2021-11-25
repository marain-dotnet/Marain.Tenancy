// <copyright file="TenantProperties.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings
{
    using System.Collections.Generic;

    using Corvus.Tenancy;

    public class TenantProperties
    {
        public TenantProperties(ScenarioDiContainer diContainer)
        {
            this.DiContainer = diContainer;
        }

        public Dictionary<string, ITenant> Tenants { get; } = new Dictionary<string, ITenant>();

        public HashSet<string> TenantsToDelete { get; } = new HashSet<string>();

        public ScenarioDiContainer DiContainer { get; }
    }
}