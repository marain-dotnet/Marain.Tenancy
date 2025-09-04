// <copyright file="TenancyClientSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Steps;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Corvus.Json;
using Corvus.Json.Serialization;
using Corvus.Tenancy;
using Corvus.Testing.ReqnRoll;
using Marain.Clients;
using Marain.Clients.Hal;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Resources;
using Marain.Tenancy.Specs.Bindings;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Reqnroll;

[Binding]
public class TenancyClientSteps : Steps
{
    private ITenancyClient apiClient;

    public TenancyClientSteps(FeatureContext featureContext)
    {
        this.apiClient = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<ITenancyClient>();
    }

    [When("I use the Tenancy Client to get a tenant with id {string}")]
    public async Task WhenIUseTheTenancyClientToGetATenantWithId(string tenantId)
    {
        await this.GetTenantByIdAndStoreResponseWithHeadersAsync(tenantId, null);
    }

    [When("I use the Tenancy Client to get the tenant with id {string} and call it {string}")]
    public async Task WhenIUseTheTenancyClientToGetTheTenantWithIdAndCallIt(string tenantId, string tenantName)
    {
        await this.GetTenantByIdAndStoreResponseWithHeadersAsync(tenantId, null, tenantName);
    }

    [Given("I use the Tenancy Client to get the tenant with the id called {string} and call it {string}")]
    [When("I use the Tenancy Client to get the tenant with the id called {string} and call it {string}")]
    public async Task GivenIUseTheTenancyClientToGetTheTenantWithTheIdCalledAndCallIt(string tenantIdName, string tenantName)
    {
        string tenantId = this.ScenarioContext.Get<string>(tenantIdName);
        await this.GetTenantByIdAndStoreResponseWithHeadersAsync(tenantId, null, tenantName);
    }

    [When("I use the Tenancy Client to delete the tenant with the id called {string} that is a child of the tenant with the id called {string}")]
    public async Task WhenIDeleteTheTenantWithTheIdCalledThatIsAChildOfTheTenantWithTheIdCalled(string childTenantIdName, string parentTenantIdName)
    {
        string childTenantId = this.ScenarioContext.Get<string>(childTenantIdName);
        string parentTenantId = this.ScenarioContext.Get<string>(parentTenantIdName);

        await this.apiClient.DeleteChildTenantAsync(parentTenantId, childTenantId).ConfigureAwait(false);
        TestTenantCleanup.RemoveTenantToDelete(parentTenantId, childTenantId);
    }

    [When("I use the Tenancy Client to delete a tenant using the DeleteTenant link at position {int} from the children called {string}")]
    public async Task WhenIUseTheTenancyClientToDeleteATenantUsingTheDeleteTenantLinkAtPositionFromTheChildrenCalled(int deleteTenantIndex, string resultName)
    {
        ApiResponse<ChildTenantsResource> response = this.ScenarioContext.Get<ApiResponse<ChildTenantsResource>>(resultName);
        Assert.IsNotNull(response.Body?.Links?.DeleteTenant, $"Result {resultName} has no DeleteTenant link collection.");
        Assert.LessOrEqual(deleteTenantIndex + 1, response.Body!.Links!.DeleteTenant!.Count);
        string link = response.Body!.Links!.DeleteTenant[deleteTenantIndex].Href!;

        string[] linkSegments = link.Split('/', StringSplitOptions.RemoveEmptyEntries);
        TestTenantCleanup.RemoveTenantToDelete(linkSegments[0], linkSegments[^1]);

        await this.apiClient.DeleteChildTenantByLinkAsync(link).ConfigureAwait(false);
    }

    [Given("I get the children of the tenant called {string} using the children link and call them {string}")]
    public async Task GivenIGetTheChildrenOfTheTenantCalledUsingTheChildrenLinkAndCallThem(string tenantName, string resultName)
    {
        ApiResponse<TenantResource> tenant = this.ScenarioContext.Get<ApiResponse<TenantResource>>(tenantName);
        string link = tenant.Body?.Links?.Children?.Href ?? throw new InvalidOperationException($"The tenant with name {tenantName} does not have a children link.");
        ApiResponse<ChildTenantsResource> response = await this.apiClient.GetChildrenByLinkAsync(link).ConfigureAwait(false);
        this.ScenarioContext.Set(response, resultName);
    }

    [Given("I use the Tenancy Client to create a child tenant called {string} for the root tenant")]
    public async Task GivenICreateAChildTenantCalledForTheRootTenant(string tenantName)
    {
        ApiResponse<TenantResource> response = await this.apiClient.CreateChildTenantAsync(RootTenant.RootTenantId, tenantName).ConfigureAwait(false);

        TestTenantCleanup.AddTenantToDelete(RootTenant.RootTenantId, response.Body.Id);
        this.ScenarioContext.Set(response, tenantName);
    }

    [When("I use the Tenancy Client to create a child tenant called {string} for the tenant with Id {string}")]
    public async Task WhenIUseTheTenancyClientToCreateAChildTenantCalledForTheTenantWithId(string tenantName, string parentTenantId)
    {
        await CommonSteps.ExecuteAndStoreExceptionIfThrownAsync(
            async () =>
            {
                ApiResponse<TenantResource> response = await this.apiClient.CreateChildTenantAsync(parentTenantId, tenantName).ConfigureAwait(false);

                TestTenantCleanup.AddTenantToDelete(RootTenant.RootTenantId, response.Body.Id);
                this.ScenarioContext.Set(response, tenantName);
            },
            this.ScenarioContext).ConfigureAwait(false);
    }

    [When("I use the Tenancy Client to get the tenant with the id called {string} and the ETag called {string}")]
    public async Task WhenIUseTheTenancyClientToGetTheTenantWithTheIdAndTheETagCalled(string tenantIdName, string tenantETagName)
    {
        string etag = this.ScenarioContext.Get<string?>(tenantETagName) ?? throw new ArgumentException($"The ETag called {tenantETagName} is null");
        string tenantId = this.ScenarioContext.Get<string>(tenantIdName);

        await this.GetTenantByIdAndStoreResponseWithHeadersAsync(tenantId, etag);
    }

    [Given("I use the Tenancy Client to update the properties of the tenant called {string}")]
    [When("I use the Tenancy Client to update the properties of the tenant called {string}")]
    public async Task WhenIUseTheTenancyClientToUpdateThePropertiesOfTheTenantCalled(string tenantName, DataTable dataTable)
    {
        IServiceProvider serviceProvider = ContainerBindings.GetServiceProvider(this.FeatureContext);
        IJsonSerializerOptionsProvider serializerOptionsProvider = serviceProvider.GetRequiredService<IJsonSerializerOptionsProvider>();

        ApiResponse<TenantResource> tenant = this.ScenarioContext.Get<ApiResponse<TenantResource>>(tenantName);

        (IDictionary<string, object>? propertiesToAddOrUpdate, IEnumerable<string>? propertiesToRemove) = CommonSteps.DataTableToUpdateTenantJsonPatchEntry(dataTable);

        await this.apiClient.UpdateTenantAsync(tenant.Body.Id, null, propertiesToAddOrUpdate, propertiesToRemove).ConfigureAwait(false);
    }

    [When("I use the Tenancy Client to rename the tenant called {string} to {string} and update its properties")]
    public async Task WhenIUseTheTenancyClientToRenameTheTenantCalledToAndUpdateItsProperties(string tenantName, string newName, DataTable dataTable)
    {
        IServiceProvider serviceProvider = ContainerBindings.GetServiceProvider(this.FeatureContext);
        IJsonSerializerOptionsProvider serializerOptionsProvider = serviceProvider.GetRequiredService<IJsonSerializerOptionsProvider>();

        ApiResponse<TenantResource> tenant = this.ScenarioContext.Get<ApiResponse<TenantResource>>(tenantName);

        (IDictionary<string, object>? propertiesToAddOrUpdate, IEnumerable<string>? propertiesToRemove) = CommonSteps.DataTableToUpdateTenantJsonPatchEntry(dataTable);

        try
        {
            await this.apiClient.UpdateTenantAsync(tenant.Body.Id, newName, propertiesToAddOrUpdate, propertiesToRemove).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            CommonSteps.SetLastException(this.ScenarioContext, e);
        }
    }

    [When("I use the Tenancy Client to update the properties of the tenant with id {string}")]
    public async Task WhenIUseTheTenancyClientToUpdateThePropertiesOfTheTenantWithId(string tenantId, DataTable dataTable)
    {
        IServiceProvider serviceProvider = ContainerBindings.GetServiceProvider(this.FeatureContext);
        IJsonSerializerOptionsProvider serializerOptionsProvider = serviceProvider.GetRequiredService<IJsonSerializerOptionsProvider>();

        (IDictionary<string, object>? propertiesToAddOrUpdate, IEnumerable<string>? propertiesToRemove) = CommonSteps.DataTableToUpdateTenantJsonPatchEntry(dataTable);

        try
        {
            await this.apiClient.UpdateTenantAsync(tenantId, null, propertiesToAddOrUpdate, propertiesToRemove).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            CommonSteps.SetLastException(this.ScenarioContext, e);
        }
    }

    [Given("I use the Tenancy Client to get the children of the tenant with the id called {string} with maxItems {int} and call them {string}")]
    [When("I use the Tenancy Client to get the children of the tenant with the id called {string} with maxItems {int} and call them {string}")]
    public async Task WhenIGetTheChildrenOfTheTenantWithTheIdCalledWithMaxItemsAndCallThem(string tenantIdName, int maxItems, string resultName)
    {
        string tenantId = this.ScenarioContext.Get<string>(tenantIdName);

        ApiResponse<ChildTenantsResource> response = await this.apiClient.GetChildrenAsync(tenantId, null, maxItems).ConfigureAwait(false);

        this.ScenarioContext.Set(response, resultName);
    }

    [When("I use the Tenancy Client to get additional children using the next link from the children called {string} and call them {string}")]
    public async Task WhenIUseTheTenancyClientToGetAdditionalChildrenUsingTheNextLinkFromTheChildrenCalledAndCallThem(string childrenName, string resultName)
    {
        ApiResponse<ChildTenantsResource> previousResponse = this.ScenarioContext.Get<ApiResponse<ChildTenantsResource>>(childrenName);
        string link = previousResponse.Body.Links?.Next?.Href ?? throw new InvalidOperationException($"The response named '{childrenName}' does not contain a next link.");

        ApiResponse<ChildTenantsResource> response = await this.apiClient.GetChildrenByLinkAsync(link).ConfigureAwait(false);

        this.ScenarioContext.Set(response, resultName);
    }

    [When("I use the Tenancy Client to get the children of the tenant with the id called {string} with maxItems {int} and continuation token from the children called {string} and call them {string}")]
    public async Task WhenIUseTheTenancyClientToGetTheChildrenOfTheTenantWithTheIdCalledWithMaxItemsAndContinuationTokenFromTheChildrenCalledAndCallThem(string tenantIdName, int maxItems, string continuationTokenResultName, string resultName)
    {
        string tenantId = this.ScenarioContext.Get<string>(tenantIdName);
        ApiResponse<ChildTenantsResource> previousResponse = this.ScenarioContext.Get<ApiResponse<ChildTenantsResource>>(continuationTokenResultName);
        string continuationToken = previousResponse.Body?.ContinuationToken ?? throw new InvalidOperationException($"The response named '{continuationTokenResultName}' does not have a continuation token.");

        ApiResponse<ChildTenantsResource> response = await this.apiClient.GetChildrenAsync(tenantId, continuationToken, maxItems).ConfigureAwait(false);
        this.ScenarioContext.Set(response, resultName);
    }

    [Then("the tenant called {string} should have no properties")]
    public void ThenTheTenantCalledShouldHaveNoProperties(string tenantName)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponse<TenantResource> response = this.ScenarioContext.Get<ApiResponse<TenantResource>>(tenantName);
        Assert.AreEqual(0, response.Body?.Properties?.AsDictionary().Count ?? 0);
    }

    [Then("the tenant called {string} should have a self link with path {string}")]
    public void ThenTheTenantCalledShouldHaveASelfLinkWithValue(string tenantName, string expectedPath)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponse<TenantResource> response = this.ScenarioContext.Get<ApiResponse<TenantResource>>(tenantName);
        Assert.IsNotNull(response.Body?.Links?.Self?.Href, $"Tenant '{tenantName}' does not contain a self link.");
        Assert.IsTrue(response.Body!.Links!.Self!.Href!.EndsWith(expectedPath));
    }

    [Then("the tenant called {string} should have a children link with path {string}")]
    public void ThenTheTenantCalledShouldHaveAChildrenLinkWithValue(string tenantName, string expectedPath)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponse<TenantResource> response = this.ScenarioContext.Get<ApiResponse<TenantResource>>(tenantName);
        Assert.IsNotNull(response.Body?.Links?.Children?.Href, $"Tenant '{tenantName}' does not contain a children link.");
        Assert.IsTrue(response.Body!.Links!.Children!.Href!.EndsWith(expectedPath));
    }

    [Then("the children called {string} should contain a self link")]
    public void ThenTheChildrenCalledShouldContainASelfLink(string childrenName)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponse<ChildTenantsResource> response = this.ScenarioContext.Get<ApiResponse<ChildTenantsResource>>(childrenName);
        Assert.IsNotNull(response.Body?.Links?.Self);
    }

    [Then("the children called {string} should contain a next link with no value")]
    public void ThenTheChildrenCalledShouldContainANextLinkWithNoValue(string childrenName)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponse<ChildTenantsResource> response = this.ScenarioContext.Get<ApiResponse<ChildTenantsResource>>(childrenName);
        Assert.IsNull(response.Body?.Links?.Next);
    }

    [Then("the children called {string} should have the MaxItems property set to {int}")]
    public void ThenTheChildrenCalledShouldHaveTheMaxItemsPropertySetTo(string childrenName, int expectedMaxItems)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponse<ChildTenantsResource> response = this.ScenarioContext.Get<ApiResponse<ChildTenantsResource>>(childrenName);
        Assert.AreEqual(expectedMaxItems, response.Body?.MaxItems);
    }

    [Then("there should be no links in the GetTenants link collection of the children called {string}")]
    public void ThenThereShouldBeNoLinksInTheGetTenantsLinkCollectionOfTheChildrenCalled(string resultName)
    {
        ApiResponse<ChildTenantsResource> result = this.ScenarioContext.Get<ApiResponse<ChildTenantsResource>>(resultName);
        Assert.AreEqual(0, result.Body?.Links?.GetTenant?.Count);
    }

    [Then("the links in the GetTenants link collection of the children called {string} should match the self links of the tenants called")]
    public void ThenTheLinksInTheGetTenantsLinkCollectionOfTheChildrenCalledShouldMatchTheSelfLinksOfTheTenantsCalled(string resultName, DataTable dataTable)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponse<ChildTenantsResource> result = this.ScenarioContext.Get<ApiResponse<ChildTenantsResource>>(resultName);
        Assert.AreEqual(dataTable.RowCount, result.Body?.Links?.GetTenant?.Count);

        List<WebLink> childTenantLinks = result.Body!.Links!.GetTenant!;

        foreach (DataTableRow row in dataTable.Rows)
        {
            ApiResponse<TenantResource> tenant = this.ScenarioContext.Get<ApiResponse<TenantResource>>(row[0]);
            Assert.IsNotNull(tenant, $"Tenant with name '{row[0]}' not found.");

            Assert.IsTrue(childTenantLinks.Any(childLink => childLink.Href == tenant.Body?.Links?.Self?.Href));
        }
    }

    [Then("the links in the GetTenants link collections of the children called {string} and {string} should each match {int} of the self links of the tenants called")]
    public void ThenTheLinksInTheGetTenantsLinkCollectionsOfTheChildrenCalledAndShouldEachMatchOfTheSelfLinksOfTheTenantsCalled(string result1Name, string result2Name, int expectedMatchingItemsPerResult, DataTable dataTable)
   {
        ApiResponse<ChildTenantsResource> result1 = this.ScenarioContext.Get<ApiResponse<ChildTenantsResource>>(result1Name);
        Assert.AreEqual(expectedMatchingItemsPerResult, result1.Body?.Links?.GetTenant?.Count);

        ApiResponse<ChildTenantsResource> result2 = this.ScenarioContext.Get<ApiResponse<ChildTenantsResource>>(result2Name);
        Assert.AreEqual(expectedMatchingItemsPerResult, result2.Body?.Links?.GetTenant?.Count);

        List<WebLink> childTenantLinks = [.. result1.Body!.Links!.GetTenant!, .. result2.Body!.Links!.GetTenant!];

        foreach (DataTableRow row in dataTable.Rows)
        {
            ApiResponse<TenantResource> tenant = this.ScenarioContext.Get<ApiResponse<TenantResource>>(row[0]);
            Assert.IsNotNull(tenant, $"Tenant with name '{row[0]}' not found.");

            Assert.IsTrue(childTenantLinks.Any(childLink => childLink.Href == tenant.Body?.Links?.Self?.Href));
        }
    }

    [Then("the links in the GetTenants link collection of the children called {string} should contain {int} items")]
    public void ThenTheLinksInTheGetTenantsLinkCollectionOfTheChildrenCalledShouldContainItems(string resultName, int expectedItems)
    {
        ApiResponse<ChildTenantsResource> result = this.ScenarioContext.Get<ApiResponse<ChildTenantsResource>>(resultName);
        Assert.AreEqual(expectedItems, result.Body?.Links?.GetTenant?.Count);
    }

    [Then("the tenant response called {string} was retrieved from the cache")]
    public void ThenTheTenantResponseCalledWasRetrievedFromTheCache(string resultName)
    {
        ApiResponse<TenantResource> result = this.ScenarioContext.Get<ApiResponse<TenantResource>>(resultName);

        // Get the CacheCow header
        result.Headers.TryGetValue("x-cachecow-client", out string? cacheCowHeader);
        Assert.IsNotNull(cacheCowHeader, "x-cachecow-client header is not present in the response");

        // We expect to see "retrieved-from-cache=True" if the item was retrieved from the cache.
        int index = cacheCowHeader!.IndexOf("retrieved-from-cache=True", StringComparison.OrdinalIgnoreCase);
        Assert.IsTrue(index >= 0, $"CacheCow header value [{cacheCowHeader}] shows the tenant was not retrieved from the cache.");
    }

    private async Task GetTenantByIdAndStoreResponseWithHeadersAsync(string tenantId, string? etag, string? name = null)
    {
        try
        {
            ApiResponse<TenantResource> response = await this.apiClient.GetTenantAsync(tenantId, etag).ConfigureAwait(false);
            this.ScenarioContext.Set(response, name ?? tenantId);
        }
        catch (Exception ex)
        {
            CommonSteps.SetLastException(this.ScenarioContext, ex);
        }
    }
}