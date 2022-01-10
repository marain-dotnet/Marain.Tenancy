// <copyright file="DeleteTenantSteps.cs" company="Endjin Limited">
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
    public class DeleteTenantSteps : TenantStepsBase
    {
        private Exception? deletionException;

        public DeleteTenantSteps(TenantProperties tenantProperties)
            : base(tenantProperties)
        {
        }

        [When("I delete the tenant with the id from label '([^']*)'")]
        public async Task GivenIDeleteATenant(string tenantLabel)
        {
            ITenant existingTenant = this.Tenants[tenantLabel];
            await this.TenantStore.DeleteTenantAsync(existingTenant.Id);
            this.TenantNoLongerRequiresDeletion(existingTenant.Id);
        }

        [When("I attempt to delete a tenant with the id that looks like a child of the root tenant but which does not exist")]
        public async Task WhenIAttemptToDeleteATenantWithTheIdThatLooksLikeAChildOfTheRootTenantButWhichDoesNotExist()
        {
            await this.AttemptToDelete(TenantExtensions.CreateChildId(RootTenant.RootTenantId, Guid.NewGuid()));
        }

        [When("I attempt to delete a tenant with the id that looks like a child of a child of the root tenant, where neither exists")]
        public async Task WhenIAttemptToDeleteATenantWithTheIdThatLooksLikeAChildOfAChildOfTheRootTenantWhereNeitherExistsAsync()
        {
            string nonExistentParent = TenantExtensions.CreateChildId(RootTenant.RootTenantId, Guid.NewGuid());
            await this.AttemptToDelete(TenantExtensions.CreateChildId(nonExistentParent, Guid.NewGuid()));
        }

        [When("I attempt to delete a tenant with the id that looks like a child of '([^']*)' but which does not exist")]
        public async Task WhenIAttemptToDeleteATenantWithTheIdThatLooksLikeAChildOfButWhichDoesNotExistAsync(string parentTenantLabel)
        {
            ITenant parent = this.Tenants[parentTenantLabel];
            await this.AttemptToDelete(TenantExtensions.CreateChildId(parent.Id, Guid.NewGuid()));
        }

        [When("I attempt to delete the root tenant")]
        public async Task WhenIAttemptToDeleteTheRootTenantAsync()
        {
            await this.AttemptToDelete(RootTenant.RootTenantId);
        }

        [When("I attempt to delete the tenant with the id from label '([^']*)'")]
        public async Task WhenIAttemptToDeleteTheTenantWithTheIdFromLabelAsync(string parentTenantLabel)
        {
            ITenant parentTenant = this.Tenants[parentTenantLabel];
            await this.AttemptToDelete(parentTenant.Id);
        }

        [Then("the attempt to delete a tenant should throw a TenantNotFoundException")]
        public void ThenTheAttemptToDeleteATenantShouldThrowATenantNotFoundException()
        {
            Assert.IsInstanceOf<TenantNotFoundException>(this.deletionException);
        }

        [Then("the attempt to delete a tenant should throw an ArgumentException")]
        public void ThenTheAttemptToDeleteATenantShouldThrowAnArgumentException()
        {
            Assert.IsInstanceOf<ArgumentException>(this.deletionException);
        }

        private async Task AttemptToDelete(string tenantId)
        {
            try
            {
                await this.TenantStore.DeleteTenantAsync(tenantId);
                this.TenantNoLongerRequiresDeletion(tenantId);
            }
            catch (Exception x)
            {
                this.deletionException = x;
            }
        }
    }
}