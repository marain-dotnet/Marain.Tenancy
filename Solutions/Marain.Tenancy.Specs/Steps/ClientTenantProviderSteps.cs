// <copyright file="ClientTenantProviderSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Steps;

using System.Collections.Generic;
using System.Threading.Tasks;
using Corvus.Json.PropertyBag;
using Corvus.Tenancy;
using Corvus.Testing.ReqnRoll;
using Marain.Tenancy.Specs.Bindings;

using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Reqnroll;

[Binding]
public class ClientTenantProviderSteps : Steps
{
    private readonly ITenantStore store;
    private readonly IJsonPropertyBagFactory propertyBagFactory;

    public ClientTenantProviderSteps(
        FeatureContext featureContext,
        ScenarioContext scenarioContext)
    {
        this.store = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<ITenantStore>();
        this.propertyBagFactory = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<IJsonPropertyBagFactory>();
    }

    [When("I use the ClientTenantProvider to get a tenant with id {string}")]
    public async Task WhenIGetATenantWithId(string tenantId)
    {
        await CommonSteps.ExecuteAndStoreExceptionIfThrownAsync(
            async () => await this.store.GetTenantAsync(tenantId).ConfigureAwait(false),
            this.ScenarioContext);
    }

    [Given("I use the ClientTenantProvider to create a child tenant called {string} for the root tenant")]
    [When("I use the ClientTenantProvider to create a child tenant called {string} for the root tenant")]
    public async Task WhenIUseTheClientTenantProviderToCreateAChildTenantCalledForTheRootTenant(string tenantName)
    {
        ITenant result = await this.store.CreateChildTenantAsync(RootTenant.RootTenantId, tenantName).ConfigureAwait(false);

        TestTenantCleanup.AddTenantToDelete(RootTenant.RootTenantId, result.Id);
        this.ScenarioContext.Set(result, tenantName);
    }

    [When("I use the ClientTenantProvider to update the properties of the tenant called {string}")]
    public async Task WhenIUseTheClientTenantProviderToUpdateThePropertiesOfTheTenantCalled(string tenantName, DataTable dataTable)
    {
        ITenant tenant = this.ScenarioContext.Get<ITenant>(tenantName);
        (IDictionary<string, object>? propertiesToAddOrUpdate, IEnumerable<string>? propertiesToRemove) = CommonSteps.DataTableToUpdateTenantJsonPatchEntry(dataTable);

        await CommonSteps.ExecuteAndStoreExceptionIfThrownAsync(
            async () => await this.store.UpdateTenantAsync(tenant.Id, null, propertiesToAddOrUpdate, propertiesToRemove),
            this.ScenarioContext);
    }

    [Given("I use the ClientTenantProvider to get the tenant with the id called {string} and call it {string}")]
    [When("I use the ClientTenantProvider to get the tenant with the id called {string} and call it {string}")]
    public async Task WhenIUseTheClientTenantProviderToGetTheTenantWithTheIdCalledAndCallIt(string tenantIdName, string tenantName)
    {
        string tenantId = this.ScenarioContext.Get<string>(tenantIdName);

        await CommonSteps.ExecuteAndStoreExceptionIfThrownAsync(
            async () =>
            {
                ITenant tenant = await this.store.GetTenantAsync(tenantId).ConfigureAwait(false);
                this.ScenarioContext.Set(tenant, tenantName);
            },
            this.ScenarioContext);
    }

    [When("I use the ClientTenantProvider to get the tenant with the id called {string} and the ETag called {string} and call it {string}")]
    public async Task WhenIUseTheClientTenantProviderToGetTheTenantWithTheIdCalledAndTheETagCalled(string tenantIdName, string tenantEtagName, string tenantName)
    {
        string tenantId = this.ScenarioContext.Get<string>(tenantIdName);
        string etag = this.ScenarioContext.Get<string>(tenantEtagName);

        await CommonSteps.ExecuteAndStoreExceptionIfThrownAsync(
            async () =>
            {
                ITenant tenant = await this.store.GetTenantAsync(tenantId, etag).ConfigureAwait(false);
                this.ScenarioContext.Set(tenant, tenantName);
            },
            this.ScenarioContext);
    }

    [When("I use the ClientTenantProvider to get the children of the tenant with the id called {string} with limit {int} and call them {string}")]
    public async Task WhenIUseTheClientTenantProviderToGetTheChildrenOfTheTenantWithTheIdCalledWithMaxItemsAndCallThem(string tenantIdName, int limit, string resultName)
    {
        string tenantId = this.ScenarioContext.Get<string>(tenantIdName);
        TenantCollectionResult result = await this.store.GetChildrenAsync(tenantId, limit).ConfigureAwait(false);
        this.ScenarioContext.Set(result, resultName);
    }

    [When("I use the ClientTenantProvider to get the children of the tenant with the id called {string} with limit {int} and continuation token from the children called {string} and call them {string}")]
    public async Task WhenIUseTheClientTenantProviderToGetTheChildrenOfTheTenantWithTheIdCalledWithLimitAndContinuationTokenFromTheChildrenCalledAndCallThem(string tenantIdName, int limit, string previousResultName, string resultName)
    {
        string tenantId = this.ScenarioContext.Get<string>(tenantIdName);
        TenantCollectionResult previousResult = this.ScenarioContext.Get<TenantCollectionResult>(previousResultName);
        TenantCollectionResult result = await this.store.GetChildrenAsync(tenantId, limit, previousResult.ContinuationToken).ConfigureAwait(false);
        this.ScenarioContext.Set(result, resultName);
    }

    [When("I use the ClientTenantProvider to delete the tenant with the id called {string}")]
    public async Task WhenIUseTheClientTenantProviderToDeleteTheTenantWithTheIdCalledThatIsAChildOfTheTenantWithTheIdCalled(string childTenantIdName)
    {
        string childTenantId = this.ScenarioContext.Get<string>(childTenantIdName);

        await CommonSteps.ExecuteAndStoreExceptionIfThrownAsync(() => this.store.DeleteTenantAsync(childTenantId), this.ScenarioContext).ConfigureAwait(false);
    }

    [Then("there should be no Ids in the list of child tenant Ids in the children called {string}")]
    public void ThenThereShouldBeNoIdsInTheGetTenantsLinkCollectionOfTheChildrenCalled(string resultName)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        TenantCollectionResult result = this.ScenarioContext.Get<TenantCollectionResult>(resultName);
        CollectionAssert.IsEmpty(result.Tenants);
    }

    [Then("the Ids in the list of child tenant Ids in the children called {string} should match the Ids of the tenants called")]
    public void ThenTheIdsInTheListOfChildTenantIdsTheChildrenCalledShouldMatchTheIdsOfTheTenantsCalled(string resultName, DataTable dataTable)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        TenantCollectionResult result = this.ScenarioContext.Get<TenantCollectionResult>(resultName);
        IList<string> childTenantIds = result.Tenants;

        Assert.AreEqual(dataTable.RowCount, childTenantIds.Count);

        foreach (DataTableRow row in dataTable.Rows)
        {
            ITenant tenant = this.ScenarioContext.Get<ITenant>(row[0]);
            Assert.IsNotNull(tenant, $"Tenant with name '{row[0]}' not found.");

            CollectionAssert.Contains(childTenantIds, tenant.Id);
        }
    }

    [Then("the Ids in the list of child tenant Ids in the children called {string} should contain {int} items")]
    public void ThenTheIdsInTheListOfChildTenantIdsInTheChildrenCalledShouldContainItems(string resultName, int expectedItemCount)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        TenantCollectionResult result = this.ScenarioContext.Get<TenantCollectionResult>(resultName);
        Assert.AreEqual(expectedItemCount, result.Tenants.Count);
    }

    [Then("the Ids in the lists of child tenant Ids in the children called {string} and {string} should each match {int} of the self Ids of the tenants called")]
    public void ThenTheIdsInTheListsOfChildTenantIdsInTheChildrenCalledAndShouldEachMatchOfTheSelfIdsOfTheTenantsCalled(string result1Name, string result2Name, int expectedMatchingItemsPerResult, DataTable dataTable)
    {
        TenantCollectionResult result1 = this.ScenarioContext.Get<TenantCollectionResult>(result1Name);
        Assert.AreEqual(expectedMatchingItemsPerResult, result1.Tenants.Count);

        TenantCollectionResult result2 = this.ScenarioContext.Get<TenantCollectionResult>(result2Name);
        Assert.AreEqual(expectedMatchingItemsPerResult, result2.Tenants.Count);

        List<string> childTenantIds = [.. result1.Tenants, .. result2.Tenants];

        foreach (DataTableRow row in dataTable.Rows)
        {
            ITenant tenant = this.ScenarioContext.Get<ITenant>(row[0]);
            Assert.IsNotNull(tenant, $"Tenant with name '{row[0]}' not found.");

            CollectionAssert.Contains(childTenantIds, tenant.Id);
        }
    }
}