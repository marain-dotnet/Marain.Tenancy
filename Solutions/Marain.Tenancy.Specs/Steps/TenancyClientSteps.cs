// <copyright file="TenancyClientSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Steps;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Corvus.Json.Serialization;
using Corvus.Tenancy;
using Corvus.Testing.ReqnRoll;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Helpers;
using Marain.Tenancy.Client.Models;
using Marain.Tenancy.Specs.Bindings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using NUnit.Framework;
using Reqnroll;

[Binding]
public class TenancyClientSteps : Steps
{
    private ApiResponseWithHeaders<TenantResponse>? lastTenantResponseWithHeaders;
    private TenancyApiClient apiClient;

    public TenancyClientSteps(FeatureContext featureContext)
    {
        this.apiClient = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<TenancyApiClient>();
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

        await this.apiClient[parentTenantId].Marain.Tenant.Children[childTenantId].DeleteAsync().ConfigureAwait(false);
        TestTenantCleanup.RemoveTenantToDelete(parentTenantId, childTenantId);
    }

    [When("I use the Tenancy Client to delete a tenant using the DeleteTenant link at position {int} from the children called {string}")]
    public async Task WhenIUseTheTenancyClientToDeleteATenantUsingTheDeleteTenantLinkAtPositionFromTheChildrenCalled(int deleteTenantIndex, string resultName)
    {
        ApiResponseWithHeaders<ChildTenantsResponse> response = this.ScenarioContext.Get<ApiResponseWithHeaders<ChildTenantsResponse>>(resultName);
        Assert.IsNotNull(response.Body?.Links?.DeleteTenant, $"Result {resultName} has no DeleteTenant link collection.");
        Assert.LessOrEqual(deleteTenantIndex + 1, response.Body!.Links!.DeleteTenant!.Count);
        string link = response.Body!.Links!.DeleteTenant[deleteTenantIndex].Href!;

        string linkPath = new Uri(link).LocalPath;
        string[] linkSegments = linkPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        TestTenantCleanup.RemoveTenantToDelete(linkSegments[0], linkSegments[^1]);

        await this.apiClient[string.Empty].Marain.Tenant.Children[string.Empty].WithUrl(link).DeleteAsync().ConfigureAwait(false);
    }

    [Given("I get the children of the tenant called {string} using the children link and call them {string}")]
    public async Task GivenIGetTheChildrenOfTheTenantCalledUsingTheChildrenLinkAndCallThem(string tenantName, string resultName)
    {
        ApiResponseWithHeaders<TenantResponse> tenant = this.ScenarioContext.Get<ApiResponseWithHeaders<TenantResponse>>(tenantName);
        string link = tenant.Body?.Links?.Children?.Href ?? throw new InvalidOperationException($"The tenant with name {tenantName} does not have a children link.");
        HeadersInspectionHandlerOption headersInspectionhandler = new() { InspectResponseHeaders = true };
        ChildTenantsResponse? response = await this.apiClient[string.Empty].Marain.Tenant.Children.WithUrl(link).GetAsync(config => config.Options.Add(headersInspectionhandler)).ConfigureAwait(false);
        this.ScenarioContext.Set(new ApiResponseWithHeaders<ChildTenantsResponse>(response, headersInspectionhandler.ResponseHeaders), resultName);
    }

    [Given("I use the Tenancy Client to create a child tenant called {string} for the root tenant")]
    public async Task GivenICreateAChildTenantCalledForTheRootTenant(string tenantName)
    {
        CreateChildTenantRequest request = new() { TenantName = tenantName };
        HeadersInspectionHandlerOption headersInspectionhandler = new() { InspectResponseHeaders = true };

        TenantResponse? response = await this.apiClient[RootTenant.RootTenantId].Marain.Tenant.PostAsync(request, config => config.Options.Add(headersInspectionhandler)).ConfigureAwait(false);

        TestTenantCleanup.AddTenantToDelete(RootTenant.RootTenantId, response?.Id);
        this.ScenarioContext.Set(new ApiResponseWithHeaders<TenantResponse>(response, headersInspectionhandler.ResponseHeaders), tenantName);
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

        ApiResponseWithHeaders<TenantResponse> tenant = this.ScenarioContext.Get<ApiResponseWithHeaders<TenantResponse>>(tenantName);

        List<UpdateTenantJsonPatchEntry> updates = DataTableToUpdateTenantJsonPatchEntry(dataTable, serializerOptionsProvider.Instance);

        await this.apiClient[tenant.Body!.Id!].Marain.Tenant.PatchAsync(updates);
    }

    [When("I use the Tenancy Client to rename the tenant called {string} to {string} and update its properties")]
    public async Task WhenIUseTheTenancyClientToRenameTheTenantCalledToAndUpdateItsProperties(string tenantName, string newName, DataTable dataTable)
    {
        IServiceProvider serviceProvider = ContainerBindings.GetServiceProvider(this.FeatureContext);
        IJsonSerializerOptionsProvider serializerOptionsProvider = serviceProvider.GetRequiredService<IJsonSerializerOptionsProvider>();

        ApiResponseWithHeaders<TenantResponse> tenant = this.ScenarioContext.Get<ApiResponseWithHeaders<TenantResponse>>(tenantName);

        List<UpdateTenantJsonPatchEntry> updates = DataTableToUpdateTenantJsonPatchEntry(dataTable, serializerOptionsProvider.Instance);
        updates.Add(UpdateTenantJsonPatchEntryFactory.Create(UpdateTenantJsonPatchEntryOperation.Replace, "/name", newName));

        await this.apiClient[tenant.Body!.Id!].Marain.Tenant.PatchAsync(updates);
    }

    [When("I use the Tenancy Client to update the properties of the tenant with id {string}")]
    public async Task WhenIUseTheTenancyClientToUpdateThePropertiesOfTheTenantWithId(string tenantId, DataTable dataTable)
    {
        IServiceProvider serviceProvider = ContainerBindings.GetServiceProvider(this.FeatureContext);
        IJsonSerializerOptionsProvider serializerOptionsProvider = serviceProvider.GetRequiredService<IJsonSerializerOptionsProvider>();

        List<UpdateTenantJsonPatchEntry> updates = DataTableToUpdateTenantJsonPatchEntry(dataTable, serializerOptionsProvider.Instance);

        try
        {
            await this.apiClient[tenantId].Marain.Tenant.PatchAsync(updates);
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

        HeadersInspectionHandlerOption headersInspectionhandler = new() { InspectResponseHeaders = true };
        ChildTenantsResponse? response = await this.apiClient[tenantId].Marain.Tenant.Children.GetAsync(config =>
        {
            config.Options.Add(headersInspectionhandler);
            config.QueryParameters.MaxItems = maxItems;
        });

        this.ScenarioContext.Set(new ApiResponseWithHeaders<ChildTenantsResponse>(response, headersInspectionhandler.ResponseHeaders), resultName);
    }

    [When("I use the Tenancy Client to get additional children using the next link from the children called {string} and call them {string}")]
    public async Task WhenIUseTheTenancyClientToGetAdditionalChildrenUsingTheNextLinkFromTheChildrenCalledAndCallThem(string childrenName, string resultName)
    {
        ApiResponseWithHeaders<ChildTenantsResponse> previousResponse = this.ScenarioContext.Get<ApiResponseWithHeaders<ChildTenantsResponse>>(childrenName);
        string link = previousResponse.Body?.Links?.Next?.Href ?? throw new InvalidOperationException($"The response named '{childrenName}' does not contain a next link.");

        HeadersInspectionHandlerOption headersInspectionhandler = new() { InspectResponseHeaders = true };
        ChildTenantsResponse? response = await this.apiClient[string.Empty].Marain.Tenant.Children.WithUrl(link).GetAsync(config =>
        {
            config.Options.Add(headersInspectionhandler);
        });

        this.ScenarioContext.Set(new ApiResponseWithHeaders<ChildTenantsResponse>(response, headersInspectionhandler.ResponseHeaders), resultName);
    }

    [When("I use the Tenancy Client to get the children of the tenant with the id called {string} with maxItems {int} and continuation token from the children called {string} and call them {string}")]
    public async Task WhenIUseTheTenancyClientToGetTheChildrenOfTheTenantWithTheIdCalledWithMaxItemsAndContinuationTokenFromTheChildrenCalledAndCallThem(string tenantIdName, int maxItems, string continuationTokenResultName, string resultName)
    {
        string tenantId = this.ScenarioContext.Get<string>(tenantIdName);
        ApiResponseWithHeaders<ChildTenantsResponse> previousResponse = this.ScenarioContext.Get<ApiResponseWithHeaders<ChildTenantsResponse>>(continuationTokenResultName);
        string continuationToken = previousResponse.Body?.ContinuationToken ?? throw new InvalidOperationException($"The response named '{continuationTokenResultName}' does not have a continuation token.");

        HeadersInspectionHandlerOption headersInspectionhandler = new() { InspectResponseHeaders = true };
        ChildTenantsResponse? response = await this.apiClient[tenantId].Marain.Tenant.Children.GetAsync(config =>
        {
            config.Options.Add(headersInspectionhandler);
            config.QueryParameters.MaxItems = maxItems;
            config.QueryParameters.ContinuationToken = continuationToken;
        });

        this.ScenarioContext.Set(new ApiResponseWithHeaders<ChildTenantsResponse>(response, headersInspectionhandler.ResponseHeaders), resultName);
    }

    [Then("the tenant called {string} should have no properties")]
    public void ThenTheTenantCalledShouldHaveNoProperties(string tenantName)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponseWithHeaders<TenantResponse> response = this.ScenarioContext.Get<ApiResponseWithHeaders<TenantResponse>>(tenantName);
        Assert.AreEqual(0, response.Body?.Properties?.AdditionalData.Count ?? 0);
    }

    [Then("the tenant called {string} should have a self link with path {string}")]
    public void ThenTheTenantCalledShouldHaveASelfLinkWithValue(string tenantName, string expectedPath)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponseWithHeaders<TenantResponse> response = this.ScenarioContext.Get<ApiResponseWithHeaders<TenantResponse>>(tenantName);
        Assert.IsNotNull(response.Body?.Links?.Self?.Href, $"Tenant '{tenantName}' does not contain a self link.");
        Assert.IsTrue(response.Body!.Links!.Self!.Href!.EndsWith(expectedPath));
    }

    [Then("the tenant called {string} should have a children link with path {string}")]
    public void ThenTheTenantCalledShouldHaveAChildrenLinkWithValue(string tenantName, string expectedPath)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponseWithHeaders<TenantResponse> response = this.ScenarioContext.Get<ApiResponseWithHeaders<TenantResponse>>(tenantName);
        Assert.IsNotNull(response.Body?.Links?.Children?.Href, $"Tenant '{tenantName}' does not contain a children link.");
        Assert.IsTrue(response.Body!.Links!.Children!.Href!.EndsWith(expectedPath));
    }

    [Then("the children called {string} should contain a self link")]
    public void ThenTheChildrenCalledShouldContainASelfLink(string childrenName)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponseWithHeaders<ChildTenantsResponse> response = this.ScenarioContext.Get<ApiResponseWithHeaders<ChildTenantsResponse>>(childrenName);
        Assert.IsNotNull(response.Body?.Links?.Self);
    }

    [Then("the children called {string} should contain a next link with no value")]
    public void ThenTheChildrenCalledShouldContainANextLinkWithNoValue(string childrenName)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponseWithHeaders<ChildTenantsResponse> response = this.ScenarioContext.Get<ApiResponseWithHeaders<ChildTenantsResponse>>(childrenName);
        Assert.IsNull(response.Body?.Links?.Next);
    }

    [Then("the children called {string} should have the MaxItems property set to {int}")]
    public void ThenTheChildrenCalledShouldHaveTheMaxItemsPropertySetTo(string childrenName, int expectedMaxItems)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponseWithHeaders<ChildTenantsResponse> response = this.ScenarioContext.Get<ApiResponseWithHeaders<ChildTenantsResponse>>(childrenName);
        Assert.AreEqual(expectedMaxItems, response.Body?.MaxItems);
    }

    [Then("there should be no links in the GetTenants link collection of the children called {string}")]
    public void ThenThereShouldBeNoLinksInTheGetTenantsLinkCollectionOfTheChildrenCalled(string resultName)
    {
        ApiResponseWithHeaders<ChildTenantsResponse> result = this.ScenarioContext.Get<ApiResponseWithHeaders<ChildTenantsResponse>>(resultName);
        Assert.AreEqual(0, result.Body?.Links?.GetTenant?.Count);
    }

    [Then("the links in the GetTenants link collection of the children called {string} should match the self links of the tenants called")]
    public void ThenTheLinksInTheGetTenantsLinkCollectionOfTheChildrenCalledShouldMatchTheSelfLinksOfTheTenantsCalled(string resultName, DataTable dataTable)
    {
        CommonSteps.RethrowLastExceptionIfPresent(this.ScenarioContext);
        ApiResponseWithHeaders<ChildTenantsResponse> result = this.ScenarioContext.Get<ApiResponseWithHeaders<ChildTenantsResponse>>(resultName);
        Assert.AreEqual(dataTable.RowCount, result.Body?.Links?.GetTenant?.Count);

        List<LinkResponse> childTenantLinks = result.Body!.Links!.GetTenant!;

        foreach (DataTableRow row in dataTable.Rows)
        {
            ApiResponseWithHeaders<TenantResponse> tenant = this.ScenarioContext.Get<ApiResponseWithHeaders<TenantResponse>>(row[0]);
            Assert.IsNotNull(tenant, $"Tenant with name '{row[0]}' not found.");

            Assert.IsTrue(childTenantLinks.Any(childLink => childLink.Href == tenant.Body?.Links?.Self?.Href));
        }
    }

    [Then("the links in the GetTenants link collections of the children called {string} and {string} should each match {int} of the self links of the tenants called")]
    public void ThenTheLinksInTheGetTenantsLinkCollectionsOfTheChildrenCalledAndShouldEachMatchOfTheSelfLinksOfTheTenantsCalled(string result1Name, string result2Name, int expectedMatchingItemsPerResult, DataTable dataTable)
   {
        ApiResponseWithHeaders<ChildTenantsResponse> result1 = this.ScenarioContext.Get<ApiResponseWithHeaders<ChildTenantsResponse>>(result1Name);
        Assert.AreEqual(expectedMatchingItemsPerResult, result1.Body?.Links?.GetTenant?.Count);

        ApiResponseWithHeaders<ChildTenantsResponse> result2 = this.ScenarioContext.Get<ApiResponseWithHeaders<ChildTenantsResponse>>(result2Name);
        Assert.AreEqual(expectedMatchingItemsPerResult, result2.Body?.Links?.GetTenant?.Count);

        List<LinkResponse> childTenantLinks = [.. result1.Body!.Links!.GetTenant!, .. result2.Body!.Links!.GetTenant!];

        foreach (DataTableRow row in dataTable.Rows)
        {
            ApiResponseWithHeaders<TenantResponse> tenant = this.ScenarioContext.Get<ApiResponseWithHeaders<TenantResponse>>(row[0]);
            Assert.IsNotNull(tenant, $"Tenant with name '{row[0]}' not found.");

            Assert.IsTrue(childTenantLinks.Any(childLink => childLink.Href == tenant.Body?.Links?.Self?.Href));
        }
    }

    [Then("the links in the GetTenants link collection of the children called {string} should contain {int} items")]
    public void ThenTheLinksInTheGetTenantsLinkCollectionOfTheChildrenCalledShouldContainItems(string resultName, int expectedItems)
    {
        ApiResponseWithHeaders<ChildTenantsResponse> result = this.ScenarioContext.Get<ApiResponseWithHeaders<ChildTenantsResponse>>(resultName);
        Assert.AreEqual(expectedItems, result.Body?.Links?.GetTenant?.Count);
    }

    private static List<UpdateTenantJsonPatchEntry> DataTableToUpdateTenantJsonPatchEntry(DataTable dataTable, JsonSerializerOptions serializerOptions)
    {
        List<UpdateTenantJsonPatchEntry> updates = [];

        foreach ((string key, string value, string type, UpdateTenantJsonPatchEntryOperation operation) in dataTable.CreateSet<(string Key, string Value, string Type, UpdateTenantJsonPatchEntryOperation Operation)>())
        {
            if (type == "integer")
            {
                updates.Add(UpdateTenantJsonPatchEntryFactory.Create(
                    UpdateTenantJsonPatchEntryOperation.Add,
                    $"/properties/{key}",
                    int.Parse(value)));
            }
            else if (type == "datetimeoffset")
            {
                updates.Add(UpdateTenantJsonPatchEntryFactory.Create(
                    UpdateTenantJsonPatchEntryOperation.Add,
                    $"/properties/{key}",
                    DateTimeOffset.Parse(value),
                    serializerOptions));
            }
            else
            {
                updates.Add(UpdateTenantJsonPatchEntryFactory.Create(
                    UpdateTenantJsonPatchEntryOperation.Add,
                    $"/properties/{key}",
                    value));
            }
        }

        return updates;
    }

    private async Task GetTenantByIdAndStoreResponseWithHeadersAsync(string tenantId, string? etag, string? name = null)
    {
        try
        {
            HeadersInspectionHandlerOption headersInspectionhandler = new() { InspectResponseHeaders = true };

            TenantResponse? tenant = await this.apiClient[tenantId].Marain.Tenant.GetAsync(config =>
            {
                config.Options.Add(headersInspectionhandler);

                if (!string.IsNullOrEmpty(etag))
                {
                    config.Headers.Add("If-None-Match", etag);
                }
            }).ConfigureAwait(false);

            this.lastTenantResponseWithHeaders = new ApiResponseWithHeaders<TenantResponse>(tenant, headersInspectionhandler.ResponseHeaders);
            this.ScenarioContext.Set(this.lastTenantResponseWithHeaders, name ?? tenantId);
        }
        catch (Exception ex)
        {
            CommonSteps.SetLastException(this.ScenarioContext, ex);
        }
    }
}