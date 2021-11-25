// <copyright file="TenantSetupSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Corvus.Extensions.Json;
    using Corvus.Json;
    using Corvus.Storage.Azure.BlobStorage;
    using Corvus.Tenancy;

    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.MultiMode;

    using Microsoft.Extensions.DependencyInjection;

    using NUnit.Framework.Internal;

    using TechTalk.SpecFlow;

    [Binding]
    public class TenantSetupSteps : TenantStepsBase
    {
        private readonly ITenancyContainerSetup containerSetup;

        public TenantSetupSteps(TenantProperties tenantProperties)
            : base(tenantProperties)
        {
            this.SetupMode = TestExecutionContext.CurrentContext.TestObject switch
            {
                IMultiModeTest<SetupModes> multiModeTest => multiModeTest.TestType,
                _ => SetupModes.ViaApi,
            };

            this.containerSetup = this.SetupMode switch
            {
                SetupModes.ViaApi => new TenancyContainerSetupViaApi(() => this.TenantStore),
                SetupModes.DirectToStorage => new TenancyContainerSetupDirectToStorage(
                    this.DiContainer.RootBlobStorageConfiguration,
                    this.DiContainer.ServiceProvider.GetRequiredService<IBlobContainerSourceByConfiguration>(),
                    this.DiContainer.ServiceProvider.GetRequiredService<IJsonSerializerSettingsProvider>(),
                    this.DiContainer.ServiceProvider.GetRequiredService<IPropertyBagFactory>()),
                _ => throw new InvalidOperationException($"Unknown setup mode: {this.SetupMode}"),
            };
        }

        /// <summary>
        /// Gets the mode to use when setting up the containers in tests.
        /// </summary>
        public SetupModes SetupMode { get; }

        [Given("the root tenant has a child tenant called '([^']*)' labelled '([^']*)'")]
        public async Task GivenTheRootTenantHasAChildTenantCalled(
            string tenantName, string tenantLabel)
        {
            ITenant newTenant = await this.containerSetup.EnsureChildTenantExistsAsync(RootTenant.RootTenantId, tenantName);
            this.Tenants.Add(tenantLabel, newTenant);
            this.AddTenantToDelete(newTenant.Id);
        }

        [Given("the root tenant has a child tenant called '([^']*)' labelled '([^']*)' with these properties")]
        public async Task GivenTheRootTenantHasAWellKnownChildTenantWithPropertiesCalled(
            string tenantName, string tenantLabel, Table propertyTable)
        {
            ITenant newTenant = await this.containerSetup.EnsureChildTenantExistsAsync(
                RootTenant.RootTenantId, tenantName, ReadPropertiesTable(propertyTable));
            this.Tenants.Add(tenantLabel, newTenant);
            this.AddTenantToDelete(newTenant.Id);
        }

        [Given("the root tenant has a well-known child tenant called '([^']*)' with a Guid of '([^']*)' labelled '([^']*)'")]
        public async Task GivenTheRootTenantHasAWellKnownChildTenantCalled(
            string tenantName, Guid wellKnownId, string tenantLabel)
        {
            ITenant newTenant = await this.containerSetup.EnsureWellKnownChildTenantExistsAsync(
                RootTenant.RootTenantId, wellKnownId, tenantName);
            this.Tenants.Add(tenantLabel, newTenant);
            this.AddTenantToDelete(newTenant.Id);
        }

        [Given("the tenant labelled '([^']*)' has a child tenant called '([^']*)' labelled '([^']*)'")]
        public async Task GivenTheRootTenantHasAChildTenantCalled(
            string parentTenantLabel, string tenantName, string newTenantLabel)
        {
            ITenant parent = this.Tenants[parentTenantLabel];
            ITenant newTenant = await this.containerSetup.EnsureChildTenantExistsAsync(parent.Id, tenantName);
            this.Tenants.Add(newTenantLabel, newTenant);
            this.AddTenantToDelete(newTenant.Id);
        }

        [Given("the tenant labelled '([^']*)' has a well-known child tenant called '([^']*)' with a Guid of '([^']*)' labelled '([^']*)'")]
        public async Task GivenTheRootTenantHasAChildTenantCalled(
            string parentTenantLabel, string tenantName, Guid wellKnownId, string newTenantLabel)
        {
            ITenant parent = this.Tenants[parentTenantLabel];
            ITenant newTenant = await this.containerSetup.EnsureWellKnownChildTenantExistsAsync(
                parent.Id, wellKnownId, tenantName);
            this.Tenants.Add(newTenantLabel, newTenant);
            this.AddTenantToDelete(newTenant.Id);
        }

        [AfterScenario("@withBlobStorageTenantProvider")]
        public async Task DeleteTenantsCreatedByTests()
        {
            // We delete tenants with longer names first to ensure that we delete child tenants before their
            // parents, as otherwise, things go wrong.
            foreach (string tenantId in this.TenantsToDelete.OrderByDescending(id => id.Length))
            {
                await this.TenantStore.DeleteTenantAsync(tenantId).ConfigureAwait(false);
            }
        }
    }
}