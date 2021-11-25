// <copyright file="RenameTenantSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.StepDefinitions
{
    using System.Threading.Tasks;

    using Corvus.Tenancy;

    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;

    using TechTalk.SpecFlow;

    [Binding]
    public class RenameTenantSteps : TenantStepsBase
    {
        public RenameTenantSteps(TenantProperties tenantProperties)
            : base(tenantProperties)
        {
        }

        [When(@"I change the name of the tenant labelled '([^']*)' to '([^']*)' and label the returned tenant '([^']*)'")]
        public async Task GivenIChangeATenantNameAsync(
            string existingTenantLabel, string tenantNewName, string returnedTenantLabel)
        {
            ITenant existingTenant = this.Tenants[existingTenantLabel];
            ITenant updatedTenant = await this.TenantStore.UpdateTenantAsync(
                existingTenant.Id,
                name: tenantNewName);
            this.Tenants.Add(returnedTenantLabel, updatedTenant);
        }
   }
}