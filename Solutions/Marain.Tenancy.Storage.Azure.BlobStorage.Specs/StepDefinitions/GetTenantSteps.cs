// <copyright file="GetTenantSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.StepDefinitions
{
    using System;
    using System.Threading.Tasks;

    using Corvus.Tenancy;
    using Corvus.Tenancy.Exceptions;
    using FluentAssertions;
    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;
    using TechTalk.SpecFlow;

    [Binding]
    public sealed class GetTenantSteps : TenantStepsBase
    {
        private Exception? getTenantException;

        public GetTenantSteps(TenantProperties tenantProperties)
            : base(tenantProperties)
        {
        }

        [Given(@"I get the tenant with the id from label '([^']*)' labelled '([^']*)'")]
        [When(@"I get the tenant with the id from label '([^']*)' labelled '([^']*)'")]
        public async Task GivenIGetTheTenantUsingTheDetailsCalledAndCallItAsync(string existingLabel, string fetchedLabel)
        {
            // This is used in tests for GetTenantAsync. But it is also used in scenarios where we
            // want an initial tenant fetch to go through the tenant store so we can then pass
            // information from the results of that into further calls.
            ITenant existingTenant = this.Tenants[existingLabel];
            ITenant fetchedTenant = await this.TenantStore.GetTenantAsync(existingTenant.Id);
            this.Tenants[fetchedLabel] = fetchedTenant;
        }

        [When("I get a tenant with id '([^']*)' labelled '([^']*)'")]
        public async Task WhenIGetATenantWithId(string tenantId, string label)
        {
            this.Tenants[label] = await this.TenantStore.GetTenantAsync(tenantId);
        }

        [When("I try to get a tenant with id '([^']*)'")]
        public async Task WhenITryToGetATenantWithId(string tenantId)
        {
            try
            {
                await this.WhenIGetATenantWithId(tenantId, "WontBeUsed");
            }
            catch (Exception x)
            {
                this.getTenantException = x;
            }
        }

        [When("I get a tenant with the root id labelled '([^']*)'")]
        public async Task WhenIGetATenantWithTheRootId(string label)
        {
            await this.WhenIGetATenantWithId(RootTenant.RootTenantId, label);
        }

        [When(@"I get the tenant with the details called '([^']*)' labelled '([^']*)'")]
        public async Task WhenIGetTheTenantWithTheDetailsCalledAsync(string existingLabel, string label)
        {
            ITenant tenant = this.Tenants[existingLabel];
            await this.WhenIGetATenantWithId(tenant.Id, label);
        }

        [When(@"I try to get the tenant using the details called '([^']*)' passing the ETag")]
        public async Task WhenITryToGetTheTenantWithTheDetailsCalledPassingTheETagAsync(
            string existingLabel)
        {
            ITenant tenant = this.Tenants[existingLabel];
            tenant.ETag.Should().NotBeNull("This test depends on knowing the tenant's ETag");
            try
            {
                await this.TenantStore.GetTenantAsync(tenant.Id, tenant.ETag);
            }
            catch (Exception x)
            {
                this.getTenantException = x;
            }
        }

        [Then("the tenant details labelled '([^']*)' should have the root tenant id")]
        public void ThenTenantShouldHaveTheRootTenantId(string detailsToInspect)
        {
            ITenant tenantToInspect = this.Tenants[detailsToInspect];
            tenantToInspect.Id.Should().Be(RootTenant.RootTenantId);
        }

        [Then(@"the tenant details labelled '([^']*)' should have the root tenant name")]
        public void ThenTenantShouldHaveTheRootTenantName(string detailsToInspect)
        {
            ITenant tenantToInspect = this.Tenants[detailsToInspect];
            tenantToInspect.Name.Should().Be(RootTenant.RootTenantName);
        }

        [Then(@"the tenant details labelled '([^']*)' should match the tenant details labelled '([^']*)'")]
        public void ThenTenantShouldMatchingTheDetails(string detailsToInspect, string detailsToCompareWith)
        {
            ITenant tenantToInspect = this.Tenants[detailsToInspect];
            ITenant tenantToCompareWith = this.Tenants[detailsToCompareWith];
            tenantToInspect.Should().NotBeNull();
            tenantToInspect.Id.Should().Be(tenantToCompareWith.Id);
            tenantToInspect.Name.Should().Be(tenantToCompareWith.Name);
            tenantToInspect.ETag.Should().Be(tenantToCompareWith.ETag);
        }

        [Then(@"the tenant details labelled '([^']*)' should have tenant Id that is the hash of the Guid labelled '([^']*)'")]
        public void ThenGetTenantShouldHaveTheTenantId(string detailsToInspect, string wellKnownGuidLabel)
        {
            Guid wellKnownGuid = this.WellKnownGuids[wellKnownGuidLabel];
            string expectedId = TenantExtensions.EncodeGuid(wellKnownGuid);
            ITenant tenantToInspect = this.Tenants[detailsToInspect];
            tenantToInspect.Id.Should().Be(expectedId);
        }

        [Then(@"the tenant details labelled '([^']*)' should have tenant Id that is the concatenated hashes of the Guids labelled '([^']*)' and '([^']*)'")]
        public void ThenTheTenantDetailsLabelledShouldHaveTenantIdThatIsTheConcatenatedHashesOfTheGuidsLabelledAnd(
            string detailsToInspect, string wellKnownGuidLabel1, string wellKnownGuidLabel2)
        {
            Guid wellKnownGuid1 = this.WellKnownGuids[wellKnownGuidLabel1];
            Guid wellKnownGuid2 = this.WellKnownGuids[wellKnownGuidLabel2];
            string expectedId = TenantExtensions.EncodeGuid(wellKnownGuid1) + TenantExtensions.EncodeGuid(wellKnownGuid2);
            ITenant tenantToInspect = this.Tenants[detailsToInspect];
            tenantToInspect.Id.Should().Be(expectedId);
        }

        [Then(@"the tenant labelled '([^']*)' should have the same ID as the tenant labelled '([^']*)'")]
        public void ThenGetTenantShouldHaveTheSameTenantIdAs(string detailsToInspect, string detailsToCompareWith)
        {
            ITenant tenantToInspect = this.Tenants[detailsToInspect];
            ITenant tenantToCompareWith = this.Tenants[detailsToCompareWith];
            tenantToInspect.Id.Should().Be(tenantToCompareWith.Id);
        }

        [Then(@"the tenant labelled '([^']*)' should have the name '([^']*)'")]
        public void ThenGetTenantShouldHaveTheName(string detailsToInspect, string expectedName)
        {
            ITenant tenantToInspect = this.Tenants[detailsToInspect];
            tenantToInspect.Name.Should().Be(expectedName);
        }

        [Then("GetTenantAsync should throw an InvalidOperationException")]
        public void ThenItShouldThrowAnInvalidOperationException()
        {
            this.getTenantException.Should().BeOfType<InvalidOperationException>();
        }

        [Then("GetTenantAsync should throw a TenantNotFoundException")]
        public void ThenItShouldThrowATenantNotFoundException()
        {
            this.getTenantException.Should().BeOfType<TenantNotFoundException>();
        }

        [Then("GetTenantAsync should throw a TenantNotModifiedException")]
        public void ThenItShouldThrowATenantNotModifiedException()
        {
            this.getTenantException.Should().BeOfType<TenantNotModifiedException>();
        }
    }
}