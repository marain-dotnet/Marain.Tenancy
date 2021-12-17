// <copyright file="RenameTenantSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.StepDefinitions
{
    using System;
    using System.Threading.Tasks;

    using Corvus.Tenancy;
    using Corvus.Tenancy.Exceptions;

    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;

    using NUnit.Framework;

    using TechTalk.SpecFlow;

    [Binding]
    public class RenameTenantSteps : TenantStepsBase
    {
        private Exception? renameException;

        public RenameTenantSteps(TenantProperties tenantProperties)
            : base(tenantProperties)
        {
        }

        [When("I change the name of the tenant labelled '([^']*)' to '([^']*)' and label the returned tenant '([^']*)'")]
        public async Task GivenIChangeATenantNameAsync(
            string existingTenantLabel, string tenantNewName, string returnedTenantLabel)
        {
            ITenant existingTenant = this.Tenants[existingTenantLabel];
            ITenant updatedTenant = await this.TenantStore.UpdateTenantAsync(
                existingTenant.Id,
                name: tenantNewName);
            this.Tenants.Add(returnedTenantLabel, updatedTenant);
        }

        [When("I attempt to change the name of a tenant with the id that looks like a child of the root tenant but which does not exist")]
        public async Task WhenIAttemptToChangeTheNameOfATenantWithTheIdThatLooksLikeAChildOfTheRootTenantButWhichDoesNotExistAsync()
        {
            await this.AttemptToRename(TenantExtensions.CreateChildId(RootTenant.RootTenantId, Guid.NewGuid()));
        }

        [When("I attempt to change the name of a tenant with the id that looks like a child of a child of the root tenant, where neither exists")]
        public async Task WhenIAttemptToChangeTheNameOfATenantWithTheIdThatLooksLikeAChildOfAChildOfTheRootTenantWhereNeitherExistsAsync()
        {
            string nonExistentParent = TenantExtensions.CreateChildId(RootTenant.RootTenantId, Guid.NewGuid());
            await this.AttemptToRename(TenantExtensions.CreateChildId(nonExistentParent, Guid.NewGuid()));
        }

        [When("I attempt to change the name of a tenant with the id that looks like a child of '([^']*)' but which does not exist")]
        public async Task WhenIAttemptToChangeTheNameOfATenantWithTheIdThatLooksLikeAChildOfButWhichDoesNotExistAsync(string parentTenantLabel)
        {
            ITenant parent = this.Tenants[parentTenantLabel];
            await this.AttemptToRename(TenantExtensions.CreateChildId(parent.Id, Guid.NewGuid()));
        }

        [When("I attempt to change the name of the root tenant")]
        public async Task WhenIAttemptToChangeTheNameOfTheRootTenantAsync()
        {
            await this.AttemptToRename(RootTenant.RootTenantId);
        }

        [Then("the attempt to change the name of a tenant should throw a TenantNotFoundException")]
        public void ThenTheAttemptToChangeTheNameOfATenantShouldThrowATenantNotFoundException()
        {
            Assert.IsInstanceOf<TenantNotFoundException>(this.renameException);
        }

        [Then("the attempt to change the name of a tenant should throw an ArgumentException")]
        public void ThenTheAttemptToChangeTheNameOfATenantShouldThrowAnArgumentException()
        {
            Assert.IsInstanceOf<ArgumentException>(this.renameException);
        }

        private async Task AttemptToRename(string tenantId)
        {
            try
            {
                await this.TenantStore.UpdateTenantAsync(tenantId, name: "NewName");
            }
            catch (Exception x)
            {
                this.renameException = x;
            }
        }
    }
}