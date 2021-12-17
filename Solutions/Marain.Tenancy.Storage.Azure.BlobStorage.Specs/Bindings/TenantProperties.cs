// <copyright file="TenantProperties.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings
{
    using System;
    using System.Collections.Generic;

    using Corvus.Tenancy;

    public class TenantProperties
    {
        public TenantProperties(ScenarioDiContainer diContainer)
        {
            this.DiContainer = diContainer;
        }

        public Dictionary<string, ITenant> Tenants { get; } = new Dictionary<string, ITenant>();

        public Dictionary<string, Guid> WellKnownGuids { get; } = new ();

        public HashSet<string> TenantsToDelete { get; } = new HashSet<string>();

        public HashSet<string> WellKnownTenantsToDelete { get; } = new HashSet<string>();

        public ScenarioDiContainer DiContainer { get; }
    }
}