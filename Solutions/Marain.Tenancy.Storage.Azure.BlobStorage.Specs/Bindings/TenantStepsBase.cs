﻿// <copyright file="TenantStepsBase.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Corvus.Tenancy;

    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.MultiMode;

    using TechTalk.SpecFlow;

    public abstract class TenantStepsBase
    {
        private readonly TenantProperties tenantProperties;

        protected TenantStepsBase(TenantProperties tenantProperties)
        {
            this.tenantProperties = tenantProperties;
        }

        public ScenarioDiContainer DiContainer => this.tenantProperties.DiContainer;

        public Dictionary<string, ITenant> Tenants => this.tenantProperties.Tenants;

        public HashSet<string> TenantsToDelete => this.tenantProperties.TenantsToDelete;

        public Dictionary<string, Guid> WellKnownGuids => this.tenantProperties.WellKnownGuids;

        public HashSet<string> WellKnownTenantsToDelete => this.tenantProperties.WellKnownTenantsToDelete;

        public ITenantStore TenantStore => this.DiContainer.TenantStore;

        /// <summary>
        /// Gets the mode to use when setting up the containers in tests.
        /// </summary>
        public SetupModes SetupMode => this.DiContainer.SetupMode;

        /// <summary>
        /// Gets a value indicating whether the root tenancy configuration is propagated in V2 or
        /// V3 style.
        /// </summary>
        public bool PropagateRootTenancyStorageConfigAsV2 => this.DiContainer.PropagateRootTenancyStorageConfigAsV2;

        public void AddTenantToDelete(string id)
        {
            this.TenantsToDelete.Add(id);
        }

        public void AddWellKnownTenantToDelete(string id)
        {
            this.WellKnownTenantsToDelete.Add(id);
        }

        public void TenantNoLongerRequiresDeletion(string id)
        {
            this.TenantsToDelete.Remove(id);
        }

        protected static IEnumerable<KeyValuePair<string, object>> ReadPropertiesTable(Table propertyTable)
        {
            return propertyTable.Rows.Select(row =>
            {
                string value = row["Value"];
                object interprettedValue = row["Type"] switch
                {
                    "string" => value,
                    "integer" => int.Parse(value),
                    "datetimeoffset" => DateTimeOffset.Parse(value),
                    string t => throw new InvalidOperationException($"Unknown type {t} in test table"),
                };
                return new KeyValuePair<string, object>(row["Key"], interprettedValue);
            });
        }
    }
}