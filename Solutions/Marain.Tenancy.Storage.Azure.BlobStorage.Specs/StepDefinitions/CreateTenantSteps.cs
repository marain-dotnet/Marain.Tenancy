// <copyright file="CreateTenantSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.StepDefinitions
{
    using System;
    using System.Threading.Tasks;

    using Corvus.Storage.Azure.BlobStorage;
    using Corvus.Storage.Azure.BlobStorage.Tenancy;
    using Corvus.Tenancy;

    using FluentAssertions;

    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;
    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.MultiMode;

    using TechTalk.SpecFlow;

    [Binding]
    public class CreateTenantSteps : TenantStepsBase
    {
        private Exception? createWellKnownChildTenantException;

        public CreateTenantSteps(TenantProperties tenantProperties)
            : base(tenantProperties)
        {
        }

        [When(@"I create a child tenant of the root tenant called '([^']*)' labelled '([^']*)'")]
        public async Task GivenICreateAChildOfTheRootTenantCalledWithTheDetailsAvailableAsAsync(
            string tenantName, string newTenantLabel)
        {
            // This is called in scenarios where we want the tenant creation to go through the
            // tenant store under test even when the setup mode is direct-to-store, because we're
            // validating that the information returned from the store works when we use it again
            // in a fetch. (E.g., one of the tests we do for ETag-based gets is to check that if
            // we create via the store, and then get with the id and ETag returned by the store,
            // we get a TenantNotModifiedException.)
            ITenant newTenant = await this.TenantStore.CreateChildTenantAsync(RootTenant.RootTenantId, tenantName);
            this.Tenants.Add(newTenantLabel, newTenant);
            this.AddTenantToDelete(newTenant.Id);
        }

        [When(@"I create a well known child tenant of the root tenant called '([^']*)' with a Guid of '([^']*)' labelled '([^']*)'")]
        public async Task GivenICreateAWellKnownChildOfTheRootTenantCalledWithTheDetailsAvailableAsAsync(
            string tenantName, Guid wellKnownGuid, string newTenantLabel)
        {
            // This is called in scenarios where we want the tenant creation to go through the
            // tenant store under test even when the setup mode is direct-to-store, because we're
            // validating that the information returned from the store works when we use it again
            // in a fetch. (E.g., one of the tests we do for ETag-based gets is to check that if
            // we create via the store, and then get with the id and ETag returned by the store,
            // we get a TenantNotModifiedException.)
            ITenant newTenant = await this.TenantStore.CreateWellKnownChildTenantAsync(
                RootTenant.RootTenantId, wellKnownGuid, tenantName);
            this.Tenants.Add(newTenantLabel, newTenant);
            this.AddWellKnownTenantToDelete(newTenant.Id);
        }

        [When(@"I create a child of the tenant labelled '([^']*)' named '([^']*)' labelled '([^']*)'")]
        public async Task GivenICreateAChildOfAChildOfTheRootTenantAsync(
            string parentTenantLabel, string newTenantName, string newTenantLabel)
        {
            ITenant parent = this.Tenants[parentTenantLabel];
            ITenant newTenant = await this.TenantStore.CreateChildTenantAsync(
                parent.Id, newTenantName);
            this.Tenants.Add(newTenantLabel, newTenant);
            this.AddTenantToDelete(newTenant.Id);
        }

        [When(@"I create a well known child of the tenant labelled '([^']*)' named '([^']*)' with a Guid of '([^']*)' labelled '([^']*)'")]
        public async Task GivenICreateAWellKnownChildOfAChildOfATenantAsync(
            string parentTenantLabel, string newTenantName, Guid wellKnownId, string newTenantLabel)
        {
            ITenant parent = this.Tenants[parentTenantLabel];
            ITenant newTenant = await this.TenantStore.CreateWellKnownChildTenantAsync(
                parent.Id, wellKnownId, newTenantName);
            this.Tenants.Add(newTenantLabel, newTenant);
            this.AddWellKnownTenantToDelete(newTenant.Id);
        }

        [When(@"I try to create a well known child of the tenant labelled '([^']*)' named '([^']*)' with a Guid of '([^']*)'")]
        public async Task GivenITryToCreateAWellKnownChildOfAChildOfATenantAsync(
            string parentTenantLabel, string newTenantName, Guid wellKnownId)
        {
            ITenant parent = this.Tenants[parentTenantLabel];
            try
            {
                ITenant newTenant = await this.TenantStore.CreateWellKnownChildTenantAsync(
                    parent.Id, wellKnownId, newTenantName);
            }
            catch (Exception x)
            {
                this.createWellKnownChildTenantException = x;
            }
        }

        [Then("the tenant labelled '([^']*)' should have storage configuration equivalent to the root")]
        public void TenantShouldHaveStorageConfigEquivalentToRoot(string tenantLabel)
        {
            ITenant tenant = this.Tenants[tenantLabel];
            bool propagateRootAsV2 = this.SetupMode is
                SetupModes.ViaApiPropagateRootConfigAsV2 or MultiMode.SetupModes.DirectToStoragePropagateRootConfigAsV2;

            BlobContainerConfiguration v3Config;
            if (propagateRootAsV2)
            {
                tenant.Properties.TryGet("StorageConfiguration__corvustenancy", out LegacyV2BlobStorageConfiguration v2Config)
                    .Should().BeTrue("Failed to read StorageConfiguration__corvustenancy from tenant properties");

                v3Config = LegacyConfigurationConverter.FromV2ToV3(v2Config);
            }
            else
            {
                tenant.Properties.TryGet("StorageConfigurationV3__corvustenancy", out v3Config)
                    .Should().BeTrue("Failed to read StorageConfiguration__corvustenancy from tenant properties");
            }

            v3Config.Should().BeEquivalentTo(this.DiContainer.RootBlobStorageConfiguration);
        }

        [Then("CreateWellKnownChildTenantAsync thould throw an ArgumentException")]
        public void ThenItShouldThrowAnInvalidOperationException()
        {
            this.createWellKnownChildTenantException.Should().BeOfType<ArgumentException>();
        }
    }
}