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

    using TechTalk.SpecFlow;

    [Binding]
    public class TenantSetupSteps : TenantStepsBase
    {
        private readonly ITenancyContainerSetup containerSetup;

        public TenantSetupSteps(TenantProperties tenantProperties)
            : base(tenantProperties)
        {
            this.containerSetup = this.SetupMode switch
            {
                SetupModes.ViaApiPropagateRootConfigAsV2 or SetupModes.ViaApiPropagateRootConfigAsV3 => new TenancyContainerSetupViaApi(
                    this.DiContainer.RootBlobStorageConfiguration,
                    () => this.TenantStore,
                    this.DiContainer.ServiceProvider.GetRequiredService<IBlobContainerSourceFromDynamicConfiguration>()),
                SetupModes.DirectToStoragePropagateRootConfigAsV2 or SetupModes.DirectToStoragePropagateRootConfigAsV3 => new TenancyContainerSetupDirectToStorage(
                    this.DiContainer.RootBlobStorageConfiguration,
                    this.DiContainer.ServiceProvider.GetRequiredService<IBlobContainerSourceFromDynamicConfiguration>(),
                    this.DiContainer.ServiceProvider.GetRequiredService<IJsonSerializerSettingsProvider>(),
                    this.DiContainer.ServiceProvider.GetRequiredService<IPropertyBagFactory>()),
                _ => throw new InvalidOperationException($"Unknown setup mode: {this.SetupMode}"),
            };
        }

        [Given("the root tenant has a child tenant called '([^']*)' labelled '([^']*)'")]
        public async Task GivenTheRootTenantHasAChildTenantCalled(
            string tenantName, string tenantLabel)
        {
            ITenant newTenant = await this.containerSetup.EnsureChildTenantExistsAsync(
                RootTenant.RootTenantId, tenantName, this.PropagateRootTenancyStorageConfigAsV2);
            this.Tenants.Add(tenantLabel, newTenant);
            this.AddTenantToDelete(newTenant.Id);
        }

        [Given("the root tenant has v2 a child tenant called '([^']*)' labelled '([^']*)'")]
        public async Task GivenTheRootTenantHasAv2stylChildTenantCalled(
            string tenantName, string tenantLabel)
        {
            ITenant newTenant = await this.containerSetup.EnsureChildTenantExistsAsync(
                RootTenant.RootTenantId, tenantName, true);
            this.Tenants.Add(tenantLabel, newTenant);
            this.AddTenantToDelete(newTenant.Id);
        }

        [Given("the root tenant has a child tenant called '([^']*)' labelled '([^']*)' with these properties")]
        public async Task GivenTheRootTenantHasAChildTenantWithPropertiesCalled(
            string tenantName, string tenantLabel, Table propertyTable)
        {
            ITenant newTenant = await this.containerSetup.EnsureChildTenantExistsAsync(
                RootTenant.RootTenantId, tenantName, this.PropagateRootTenancyStorageConfigAsV2, ReadPropertiesTable(propertyTable));
            this.Tenants.Add(tenantLabel, newTenant);
            this.AddTenantToDelete(newTenant.Id);
        }

        [Given("a well-known tenant Guid labelled '([^']*)'")]
        public void GivenAWell_KnownTenantGuidLabelled(string label)
        {
            // We need "well-known" ids to be different each time we run the test, because
            // otherwise, we fall foul of Azure Storage's inability to create a container
            // with the same name as a container you recently deleted.
            this.WellKnownGuids.Add(label, Guid.NewGuid());
        }

        [Given(@"the root tenant has a well-known \(from the Guid labelled '([^']*)'\) child tenant called '([^']*)' labelled '([^']*)'")]
        public async Task GivenTheRootTenantHasAWellKnownChildTenantCalled(
            string wellKnownGuidLabel, string tenantName, string tenantLabel)
        {
            Guid wellKnownId = this.WellKnownGuids[wellKnownGuidLabel];
            ITenant newTenant = await this.containerSetup.EnsureWellKnownChildTenantExistsAsync(
                RootTenant.RootTenantId, wellKnownId, tenantName, this.PropagateRootTenancyStorageConfigAsV2);
            this.Tenants.Add(tenantLabel, newTenant);

            this.AddWellKnownTenantToDelete(newTenant.Id);
        }

        [Given("the tenant labelled '([^']*)' has a child tenant called '([^']*)' labelled '([^']*)'")]
        public async Task GivenTheRootTenantHasAChildTenantCalled(
            string parentTenantLabel, string tenantName, string newTenantLabel)
        {
            ITenant parent = this.Tenants[parentTenantLabel];
            ITenant newTenant = await this.containerSetup.EnsureChildTenantExistsAsync(
                parent.Id, tenantName, this.PropagateRootTenancyStorageConfigAsV2);
            this.Tenants.Add(newTenantLabel, newTenant);
            this.AddTenantToDelete(newTenant.Id);
        }

        [Given(@"the tenant labelled '([^']*)' has a well-known \(from the Guid labelled '([^']*)'\) child tenant called '([^']*)' labelled '([^']*)'")]
        public async Task GivenTheRootTenantHasAChildTenantCalled(
            string parentTenantLabel, string wellKnownGuidLabel, string tenantName, string newTenantLabel)
        {
            Guid wellKnownId = this.WellKnownGuids[wellKnownGuidLabel];
            ITenant parent = this.Tenants[parentTenantLabel];
            ITenant newTenant = await this.containerSetup.EnsureWellKnownChildTenantExistsAsync(
                parent.Id, wellKnownId, tenantName, this.PropagateRootTenancyStorageConfigAsV2);
            this.Tenants.Add(newTenantLabel, newTenant);
            this.AddWellKnownTenantToDelete(newTenant.Id);
        }

        [AfterScenario("@withBlobStorageTenantProvider")]
        public async Task DeleteTenantsCreatedByTests()
        {
            // We delete tenants with longer names first to ensure that we delete child tenants before their
            // parents, as otherwise, things go wrong.
            foreach (string tenantId in this.TenantsToDelete.OrderByDescending(id => id.Length))
            {
                await this.containerSetup.DeleteTenantAsync(tenantId, leaveContainer: false).ConfigureAwait(false);
            }

            foreach (string tenantId in this.WellKnownTenantsToDelete.OrderByDescending(id => id.Length))
            {
                // We don't delete the container when it's a well-known tenant, because otherwise
                // we get problems with being unable to recreate a new container with the same name
                // as one we just deleted when running tests against real storage accounts.
                // So we just delete the blob.
                await this.containerSetup.DeleteTenantAsync(tenantId, leaveContainer: true).ConfigureAwait(false);
            }
        }
    }
}