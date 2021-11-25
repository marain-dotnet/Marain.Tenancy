// <copyright file="DeleteTenantSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.StepDefinitions
{
    using System.Threading.Tasks;

    using Corvus.Tenancy;

    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;

    using TechTalk.SpecFlow;

    [Binding]
    public class DeleteTenantSteps : TenantStepsBase
    {
        public DeleteTenantSteps(TenantProperties tenantProperties)
            : base(tenantProperties)
        {
        }

        [When(@"I delete the tenant with the id from label '([^']*)'")]
        public async Task GivenIDeleteATenant(string tenantLabel)
        {
            ITenant existingTenant = this.Tenants[tenantLabel];
            await this.TenantStore.DeleteTenantAsync(existingTenant.Id);
            this.TenantNoLongerRequiresDeletion(existingTenant.Id);
        }
    }
}