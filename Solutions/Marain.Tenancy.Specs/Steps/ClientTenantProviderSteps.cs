// <copyright file="ClientTenantProviderSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Steps;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using Corvus.Json.PropertyBag;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using Corvus.Testing.ReqnRoll;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Helpers;
using Marain.Tenancy.Client.Models;
using Marain.Tenancy.Specs.Bindings;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
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
        IDictionary<string, object> propertiesToAddOrUpdate = DataTableToPropertiesToAddOrUpdate(dataTable);
        IList<string> propertiesToRemove = DataTableToPropertiesToRemove(dataTable);

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

    private static IDictionary<string, object> DataTableToPropertiesToAddOrUpdate(DataTable dataTable)
    {
        Dictionary<string, object> result = [];
        IEnumerable<(string Key, string Value, string Type, UpdateTenantJsonPatchEntryOperation Operation)> rows = dataTable
            .CreateSet<(string Key, string Value, string Type, UpdateTenantJsonPatchEntryOperation Operation)>()
            .Where(x => x.Operation != UpdateTenantJsonPatchEntryOperation.Remove);

        foreach ((string key, string value, string type, UpdateTenantJsonPatchEntryOperation _) in rows)
        {
            if (type == "integer")
            {
                result.Add(key, int.Parse(value));
            }
            else if (type == "datetimeoffset")
            {
                result.Add(key, DateTimeOffset.Parse(value));
            }
            else
            {
                result.Add(key, value);
            }
        }

        return result;
    }

    private static IList<string> DataTableToPropertiesToRemove(DataTable dataTable) => dataTable
            .CreateSet<(string Key, string Value, string Type, UpdateTenantJsonPatchEntryOperation Operation)>()
            .Where(x => x.Operation == UpdateTenantJsonPatchEntryOperation.Remove)
            .Select(x => x.Key)
            .ToList();

    ////[Given("I get the tenant id of the tenant called \"(.*)\" and call it \"(.*)\"")]
    ////[When("I get the tenant id of the tenant called \"(.*)\" and call it \"(.*)\"")]
    ////public void WhenIGetTheTenantIdOfTheTenantCalledAndCallIt(string tenantName, string tenantIdName)
    ////{
    ////    ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);
    ////    this.scenarioContext.Set(tenant.Id, tenantIdName);
    ////}

    ////[Given("I get the tenant with the id called \"(.*)\" and call it \"(.*)\"")]
    ////[When("I get the tenant with the id called \"(.*)\" and call it \"(.*)\"")]
    ////public async Task WhenIGetTheTenantWithTheIdCalled(string tenantIdName, string tenantName)
    ////{
    ////    ITenant tenant = await this.store.GetTenantAsync(this.scenarioContext.Get<string>(tenantIdName)).ConfigureAwait(false);
    ////    this.scenarioContext.Set(tenant, tenantName);
    ////}

    ////[When(@"I get the tenant with id ""(.*)"" and call it ""(.*)""")]
    ////public async Task WhenIGetTheTenantWithIdAndCallItAsync(string tenantId, string tenantName)
    ////{
    ////    ITenant tenant = await this.store.GetTenantAsync(tenantId).ConfigureAwait(false);
    ////    this.scenarioContext.Set(tenant, tenantName);
    ////}

    ////[Then("the tenant called \"(.*)\" should have the same ID as the tenant called \"(.*)\"")]
    ////public void ThenTheTenantCalledShouldHaveTheSameIDAsTheTenantCalled(string firstName, string secondName)
    ////{
    ////    ITenant firstTenant = this.scenarioContext.Get<ITenant>(firstName);
    ////    ITenant secondTenant = this.scenarioContext.Get<ITenant>(secondName);
    ////    Assert.AreEqual(firstTenant.Id, secondTenant.Id);
    ////}

    ////[Then(@"the tenant called ""(.*)"" should now have the name ""(.*)""")]
    ////public void ThenTheTenantCalledShouldNowHaveTheName(
    ////    string nameTenantStoredUnder, string newTenantName)
    ////{
    ////    ITenant tenant = this.scenarioContext.Get<ITenant>(nameTenantStoredUnder);
    ////    Assert.AreEqual(newTenantName, tenant.Name);
    ////}

    ////[Then("the tenant called \"(.*)\" should have no properties")]
    ////public void ThenTheTenantCalledShouldHaveNoProperties(string tenantName)
    ////{
    ////    ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);

    ////    JsonElement properties = this.propertyBagFactory.AsJsonElement(tenant.Properties);
    ////    Assert.AreEqual(0, properties.GetArrayLength());
    ////}

    ////[Then("the tenant called \"(.*)\" should have the properties")]
    ////public void ThenTheTenantCalledShouldHaveTheProperties(string tenantName, Table table)
    ////{
    ////    ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);

    ////    foreach (DataTableRow? row in table.Rows)
    ////    {
    ////        row.TryGetValue("Key", out string key);
    ////        row.TryGetValue("Value", out string value);
    ////        row.TryGetValue("Type", out string type);
    ////        switch (type)
    ////        {
    ////            case "string":
    ////                {
    ////                    Assert.IsTrue(tenant.Properties.TryGet(key, out string actual), $"Property {key} should be present");
    ////                    Assert.AreEqual(value, actual);
    ////                    break;
    ////                }

    ////            case "integer":
    ////                {
    ////                    Assert.IsTrue(tenant.Properties.TryGet(key, out int actual), $"Property {key} should be present");
    ////                    Assert.AreEqual(int.Parse(value), actual);
    ////                    break;
    ////                }

    ////            case "datetimeoffset":
    ////                {
    ////                    Assert.IsTrue(tenant.Properties.TryGet(key, out DateTimeOffset actual), $"Property {key} should be present");
    ////                    Assert.AreEqual(DateTimeOffset.Parse(value), actual);
    ////                    break;
    ////                }

    ////            default:
    ////                throw new InvalidOperationException($"Unknown data type '{type}'");
    ////        }
    ////    }
    ////}

    ////[Given("I create a child tenant called \"(.*)\" for the root tenant")]
    ////public async Task GivenICreateAChildTenantCalledForTheRootTenant(string tenantName)
    ////{
    ////    ITenant tenant = await this.store.CreateChildTenantAsync(RootTenant.RootTenantId, tenantName).ConfigureAwait(false);
    ////    this.testTenantCleanup.AddTenantToDelete(RootTenant.RootTenantId, tenant.Id);
    ////    this.scenarioContext.Set(tenant, tenantName);
    ////}

    ////[Given("I create a child tenant called \"(.*)\" for the tenant called \"(.*)\"")]
    ////public async Task GivenICreateAChildTenantCalledForTheTenantCalled(string childName, string parentName)
    ////{
    ////    ITenant parentTenant = this.scenarioContext.Get<ITenant>(parentName);
    ////    ITenant tenant = await this.store.CreateChildTenantAsync(parentTenant.Id, childName).ConfigureAwait(false);
    ////    this.testTenantCleanup.AddTenantToDelete(parentTenant.Id, tenant.Id);
    ////    this.scenarioContext.Set(tenant, childName);
    ////}

    ////[Given("I update the properties of the tenant called \"(.*)\"")]
    ////[When("I update the properties of the tenant called \"(.*)\"")]
    ////public Task WhenIUpdateThePropertiesOfTheTenantCalled(string tenantName, Table table)
    ////{
    ////    ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);
    ////    var propertiesToSetOrAdd = new Dictionary<string, object>();

    ////    foreach (DataTableRow? row in table.Rows)
    ////    {
    ////        row.TryGetValue("Key", out string key);
    ////        row.TryGetValue("Value", out string value);
    ////        row.TryGetValue("Type", out string type);
    ////        switch (type)
    ////        {
    ////            case "string":
    ////                {
    ////                    propertiesToSetOrAdd.Add(key, value);
    ////                    break;
    ////                }

    ////            case "integer":
    ////                {
    ////                    propertiesToSetOrAdd.Add(key, int.Parse(value));
    ////                    break;
    ////                }

    ////            case "datetimeoffset":
    ////                {
    ////                    propertiesToSetOrAdd.Add(key, DateTimeOffset.Parse(value));
    ////                    break;
    ////                }

    ////            default:
    ////                throw new InvalidOperationException($"Unknown data type '{type}'");
    ////        }
    ////    }

    ////    return this.store.UpdateTenantAsync(tenant.Id, propertiesToSetOrAdd: propertiesToSetOrAdd);
    ////}

    ////[When(@"I rename the tenant called ""(.*)"" to ""(.*)"" and update its properties")]
    ////public async Task WhenIRenameTheTenantCalledToAndUpdateItsPropertiesAsync(
    ////    string tenantName,
    ////    string newTenantName,
    ////    Table table)
    ////{
    ////    ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);

    ////    var propertiesToRemove = new List<string>();
    ////    var propertiesToSetOrAdd = new Dictionary<string, object>();
    ////    foreach (DataTableRow? row in table.Rows)
    ////    {
    ////        string propertyName = row["Property"];
    ////        switch (row["Action"])
    ////        {
    ////            case "remove":
    ////                propertiesToRemove.Add(propertyName);
    ////                break;

    ////            case "addOrSet":
    ////                string value = row["Value"];
    ////                string type = row["Type"];
    ////                object actualValue = type switch
    ////                {
    ////                    "string" => value,
    ////                    "integer" => int.Parse(value),
    ////                    _ => throw new InvalidOperationException($"Unknown data type '{type}'"),
    ////                };
    ////                propertiesToSetOrAdd.Add(propertyName, actualValue);
    ////                break;

    ////            default:
    ////                Assert.Fail("Unknown action in add/modify/remove table: " + row["Action"]);
    ////                break;
    ////        }
    ////    }

    ////    await this.store.UpdateTenantAsync(
    ////        tenant.Id,
    ////        newTenantName,
    ////        propertiesToSetOrAdd.Count == 0 ? null : propertiesToSetOrAdd,
    ////        propertiesToRemove.Count == 0 ? null : propertiesToRemove).ConfigureAwait(false);
    ////}

    ////[When(@"I try to update the properties of the tenant with id ""(.*)""")]
    ////public async Task WhenITryToUpdateThePropertiesOfTheTenantWithIdAsync(string tenantId)
    ////{
    ////    ITenant tenant = await this.store.GetTenantAsync(tenantId).ConfigureAwait(false);
    ////    var propertiesToAdd = new Dictionary<string, object> { { "foo", "bar" } };
    ////    try
    ////    {
    ////        await this.store.UpdateTenantAsync(tenant.Id, propertiesToSetOrAdd: propertiesToAdd).ConfigureAwait(false);
    ////    }
    ////    catch (Exception ex)
    ////    {
    ////        this.scenarioContext.Set(ex);
    ////    }
    ////}

    ////[When("I get the children of the tenant with the id called \"(.*)\" with maxItems (.*) and call them \"(.*)\"")]
    ////public async Task WhenIGetTheChildrenOfTheTenantWithTheIdCalledWithMaxItemsAndCallThem(string tenantIdName, int maxItems, string childrenName)
    ////{
    ////    string tenantId = this.scenarioContext.Get<string>(tenantIdName);
    ////    TenantCollectionResult children = await this.store.GetChildrenAsync(tenantId, maxItems).ConfigureAwait(false);
    ////    this.scenarioContext.Set(children, childrenName);
    ////}

    ////[When("I get the children of the tenant with the id called \"(.*)\" with maxItems (.*) and continuation token \"(.*)\" and call them \"(.*)\"")]
    ////public async Task WhenIGetTheChildrenOfTheTenantWithTheIdCalledWithMaxItemsAndCallThem(string tenantIdName, int maxItems, string continuationTokenSource, string childrenName)
    ////{
    ////    string tenantId = this.scenarioContext.Get<string>(tenantIdName);
    ////    TenantCollectionResult previousChildren = this.scenarioContext.Get<TenantCollectionResult>(continuationTokenSource);
    ////    TenantCollectionResult children = await this.store.GetChildrenAsync(tenantId, maxItems, previousChildren.ContinuationToken).ConfigureAwait(false);
    ////    this.scenarioContext.Set(children, childrenName);
    ////}

    ////[Then("the ids of the children called \"(.*)\" should match the ids of the tenants called")]
    ////public void ThenTheIdsOfTheChildrenCalledShouldMatchTheIdsOfTheTenantsCalled(string childrenName, Table table)
    ////{
    ////    TenantCollectionResult children = this.scenarioContext.Get<TenantCollectionResult>(childrenName);
    ////    Assert.AreEqual(table.Rows.Count, children.Tenants.Count);
    ////    var expected = table.Rows.Select(r => this.scenarioContext.Get<ITenant>(r[0]).Id).ToList();
    ////    CollectionAssert.AreEquivalent(expected, children.Tenants);
    ////}

    ////[Then("there should be no ids in the children called \"(.*)\"")]
    ////public void ThenThereShouldBeNoIdsInTheChildrenCalled(string childrenName)
    ////{
    ////    TenantCollectionResult children = this.scenarioContext.Get<TenantCollectionResult>(childrenName);
    ////    Assert.AreEqual(0, children.Tenants.Count);
    ////}

    ////[Then("there should be (.*) tenants in \"(.*)\"")]
    ////public void ThenThereShouldBeTenantsIn(int count, string childrenName)
    ////{
    ////    TenantCollectionResult children = this.scenarioContext.Get<TenantCollectionResult>(childrenName);
    ////    Assert.AreEqual(count, children.Tenants.Count);
    ////}

    ////[Then(@"the ids of the children called ""(.*)"" and ""(.*)"" should each match (.*) of the ids of the tenants called")]
    ////public void ThenTheIdsOfTheChildrenCalledAndShouldEachMatchOfTheIdsOfTheTenantsCalled(string childrenName1, string childrenName2, int count, Table table)
    ////{
    ////    TenantCollectionResult children1 = this.scenarioContext.Get<TenantCollectionResult>(childrenName1);
    ////    TenantCollectionResult children2 = this.scenarioContext.Get<TenantCollectionResult>(childrenName2);
    ////    Assert.AreEqual(count, children1.Tenants.Count);
    ////    Assert.AreEqual(count, children2.Tenants.Count);
    ////    var expected = table.Rows.Select(r => this.scenarioContext.Get<ITenant>(r[0]).Id).ToList();
    ////    CollectionAssert.AreEquivalent(expected, children1.Tenants.Union(children2.Tenants));
    ////}

    ////[When("I delete the tenant with the id called \"(.*)\"")]
    ////public Task WhenIDeleteTheTenantWithTheIdCalled(string tenantIdName)
    ////{
    ////    string tenantId = this.scenarioContext.Get<string>(tenantIdName);
    ////    return this.store.DeleteTenantAsync(tenantId);
    ////}

    ////[Given(@"I get the ETag of the tenant called ""(.*)"" and call it ""(.*)""")]
    ////public void GivenIGetTheETagOfTheTenantCalledAndCallIt(string tenantName, string eTagName)
    ////{
    ////    ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);
    ////    this.scenarioContext.Set(tenant.ETag, eTagName);
    ////}

    ////[When(@"I use the client to get the tenant with the id called ""(.*)"" and call the response ""(.*)""")]
    ////public async Task WhenIUseTheClientToGetTheTenantWithTheIdCalledAndCallTheResponse(string tenantIdName, string responseName)
    ////{
    ////    string tenantId = this.scenarioContext.Get<string>(tenantIdName);
    ////    TenantResponse? response = await this.client.GetTenantAsync(tenantId).ConfigureAwait(false);

    ////    this.scenarioContext.Set(response, responseName);
    ////}

    ////[Then(@"the tenant response called ""(.*)"" was retrieved from the cache")]
    ////public void ThenTheTenantResponseCalledWasRetrievedFromTheCache(string responseName)
    ////{
    ////    // Note: Kiota client doesn't expose caching headers in the same way as AutoRest
    ////    // This test will need to be adapted or removed as Kiota handles caching differently
    ////    TenantResponse? response = this.scenarioContext.Get<TenantResponse?>(responseName);
    ////    Assert.IsNotNull(response, "Response should not be null");

    ////    // TODO: Implement cache validation for Kiota client if needed
    ////    // For now, we'll just verify the response exists
    ////}

    ////[When(@"I get the tenant with the id called ""(.*)"" and the ETag called ""(.*)""")]
    ////public async Task WhenIGetTheTenantWithTheIdCalledAndTheETagCalled(string tenantIdName, string tenantETagName)
    ////{
    ////    try
    ////    {
    ////        ITenant tenant = await this.store.GetTenantAsync(this.scenarioContext.Get<string>(tenantIdName), this.scenarioContext.Get<string>(tenantETagName)).ConfigureAwait(false);
    ////    }
    ////    catch (Exception ex)
    ////    {
    ////        this.scenarioContext.Set(ex);
    ////    }
    ////}

    ////[Then("it should throw a TenantNotModifiedException")]
    ////public void ThenItShouldThrowATenantNotModifiedException()
    ////{
    ////    Assert.IsInstanceOf<TenantNotModifiedException>(this.scenarioContext.Get<Exception>());
    ////}

    ////[Then("it should throw a NotSupportedException")]
    ////public void ThenItShouldThrowANotSupportedException()
    ////{
    ////    Assert.IsInstanceOf<NotSupportedException>(this.scenarioContext.Get<Exception>());
    ////}
}