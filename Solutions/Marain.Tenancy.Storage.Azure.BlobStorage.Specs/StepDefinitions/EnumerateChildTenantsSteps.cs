// <copyright file="EnumerateChildTenantsSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.StepDefinitions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Corvus.Tenancy;

    using FluentAssertions;

    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;

    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Assist;

    [Binding]
    public class EnumerateChildTenantsSteps : TenantStepsBase
    {
        private List<TenantCollectionResult> getChildrenResults = new ();

        public EnumerateChildTenantsSteps(TenantProperties tenantProperties)
            : base(tenantProperties)
        {
        }

        [When(@"I get the children of the tenant with the label '([^']*)' with maxItems (\d+)")]
        public async Task GivenIGetChildTenants(string tenantLabel, int maxItems)
        {
            ITenant existingTenant = this.Tenants[tenantLabel];
            this.getChildrenResults.Add(await this.TenantStore.GetChildrenAsync(existingTenant.Id, maxItems));
        }

        [When(@"I continue getting the children of the tenant with the label '([^']*)' with maxItems (\d+)")]
        public async Task GivenIContinueGettingChildTenants(string tenantLabel, int maxItems)
        {
            ITenant existingTenant = this.Tenants[tenantLabel];
            string? continuation = this.getChildrenResults.Last().ContinuationToken;
            this.getChildrenResults.Add(await this.TenantStore.GetChildrenAsync(existingTenant.Id, maxItems, continuation));
        }

        [Then(@"the ids of the children for page (\d+) should match these tenant ids")]
        public void IdsFromGetChildTenantsMatch(int pageIndex, Table idTable)
        {
            IEnumerable<string> ids = idTable.CreateSet(row => this.Tenants[row["TenantName"]].Id);
            this.getChildrenResults[pageIndex].Tenants.Should().BeEquivalentTo(ids);
        }

        [Then(@"the ids of all the children across all pages should match these tenant ids")]
        public void IdsFromAllGetChildTenantsMatch(Table idTable)
        {
            IEnumerable<string> expectedIds = idTable.CreateSet(row => this.Tenants[row["TenantName"]].Id);
            IEnumerable<string> actualIds = this.getChildrenResults.SelectMany(page => page.Tenants);
            actualIds.Should().BeEquivalentTo(expectedIds);
        }

        [Then(@"the continuation token for page (\d+) should be null")]
        public void ContinuationTokenForPageShouldBeNull(int pageIndex)
        {
            this.getChildrenResults[pageIndex].ContinuationToken.Should().BeNull();
        }

        [Then(@"page (\d+) returned by GetChildrenAsync should contain (\d+) items")]
        public void ContinuationTokenForPageShouldBeNull(int pageIndex, int expectedItemCount)
        {
            this.getChildrenResults[pageIndex].Tenants.Should().HaveCount(expectedItemCount);
        }

        [Then(@"the continuation token for page (\d+) should not be null")]
        public void ContinuationTokenForPageShouldNotBeNull(int pageIndex)
        {
            this.getChildrenResults[pageIndex].ContinuationToken.Should().NotBeNull();
        }
    }
}