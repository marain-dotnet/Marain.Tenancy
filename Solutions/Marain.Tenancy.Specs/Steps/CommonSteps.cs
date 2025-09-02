// <copyright file="CommonSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Steps;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Corvus.Json;
using Corvus.Tenancy;
using Corvus.Testing.ReqnRoll;
using Marain.Clients;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Resources;
using Marain.Tenancy.Specs.Bindings;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Reqnroll;

[Binding]
public class CommonSteps : Steps
{
    private const string LastExceptionKey = "LastException";

    private ITenantStore? store;
    private ITenancyClient apiClient;

    public CommonSteps(FeatureContext featureContext)
    {
        this.store = ContainerBindings.GetServiceProvider(featureContext).GetService<ITenantStore>();
        this.apiClient = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<ITenancyClient>();
    }

    public static Exception? GetLastException(ScenarioContext context)
    {
        context.TryGetValue(LastExceptionKey, out Exception? ex);
        return ex;
    }

    public static void ExecuteAndStoreExceptionIfThrown(Action action, ScenarioContext context)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            SetLastException(context, ex);
        }
    }

    public static async Task ExecuteAndStoreExceptionIfThrownAsync(Func<Task> action, ScenarioContext context)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            SetLastException(context, ex);
        }
    }

    public static void SetLastException(ScenarioContext context, Exception ex)
    {
        context.Set(ex, LastExceptionKey);
    }

    [Then("it should throw a {string}")]
    public void ThenItShouldThrowAnException(string exceptionTypeName)
    {
        Exception? lastException = GetLastException(this.ScenarioContext);
        Assert.AreEqual(exceptionTypeName, lastException?.GetType().Name);
    }

    [Then("it should throw a MarainApiException with StatusCode {string}")]
    public void ThenItShouldThrowAWithStatusCode(string expectedStatusCodeName)
    {
        Exception? lastException = GetLastException(this.ScenarioContext);
        Assert.IsInstanceOf<MarainApiException>(lastException);
        var ex = (MarainApiException)lastException!;

        HttpStatusCode expectedStatusCode = Enum.Parse<HttpStatusCode>(expectedStatusCodeName, true);
        Assert.AreEqual(expectedStatusCode, ex!.StatusCode);
    }

    public static void RethrowLastExceptionIfPresent(ScenarioContext context)
    {
        Exception? lastException = GetLastException(context);

        if (lastException is not null)
        {
            var dispatchInfo = ExceptionDispatchInfo.Capture(lastException);
            dispatchInfo.Throw();
        }
    }

    [Then("the tenant called {string} should have the name {string}")]
    public void ThenTheTenantCalledShouldHaveTheName(string tenantName, string expectedName)
    {
        this.ProcessTenantResponseBasedOnType(
            tenantName,
            tenant => Assert.AreEqual(expectedName, tenant?.Body?.Name),
            tenant => Assert.AreEqual(expectedName, tenant.Name));
    }

    [Given("I get the tenant id of the tenant called {string} and call it {string}")]
    [When("I get the tenant id of the tenant called {string} and call it {string}")]
    public void WhenIGetTheTenantIdOfTheTenantCalledAndCallIt(string tenantName, string tenantIdName)
    {
        this.ProcessTenantResponseBasedOnType(
            tenantName,
            tenant => this.ScenarioContext.Set(tenant.Body?.Id, tenantIdName),
            tenant => this.ScenarioContext.Set(tenant.Id, tenantIdName));
    }

    [Given("I get the ETag of the tenant called {string} and call it {string}")]
    public void GivenIGetTheETagOfTheTenantCalledAndCallIt(string tenantName, string tenantETagName)
    {
        this.ProcessTenantResponseBasedOnType(
            tenantName,
            tenant =>
            {
                tenant.Headers.TryGetValue("ETag", out string? etag);
                this.ScenarioContext.Set(etag, tenantETagName);
            },
            tenant => this.ScenarioContext.Set(tenant.ETag, tenantETagName));
    }

    [Then("the tenant called {string} should have the same ID as the tenant called {string}")]
    public void ThenTheTenantCalledShouldHaveTheSameIDAsTheTenantCalled(string rightTenantName, string leftTenantName)
    {
        this.ProcessTenantResponsesBasedOnType(
            [leftTenantName, rightTenantName],
            tenants => Assert.AreEqual(tenants[0].Body!.Id, tenants[1].Body!.Id),
            tenants => Assert.AreEqual(tenants[0].Id, tenants[1].Id));
    }

    [Given("I create a child tenant called {string} for the tenant called {string}")]
    public async Task GivenICreateAChildTenantCalledForTheTenantCalled(string childName, string parentName)
    {
        await this.ProcessTenantResponseBasedOnTypeAsync(
            parentName,
            async parentTenant =>
            {
                Assert.IsNotNull(parentTenant.Body);

                ApiResponse<TenantResource> response = await this.apiClient.CreateChildTenantAsync(parentTenant.Body.Id, childName).ConfigureAwait(false);

                TestTenantCleanup.AddTenantToDelete(parentTenant.Body.Id, response.Body.Id);

                this.ScenarioContext.Set(response, childName);
            },
            async parentTenant =>
            {
                ITenant response = await this.store!.CreateChildTenantAsync(parentTenant.Id, childName);
                TestTenantCleanup.AddTenantToDelete(parentTenant.Id, response.Id);
                this.ScenarioContext.Set(response, childName);
            });
    }

    [Then("the children called {string} should have the ContinuationToken property set to null")]
    public void ThenTheChildrenCalledShouldHaveTheContinuationTokenPropertySetToNull(string childrenName)
    {
        this.ProcessGetChildrenResponseBasedOnType(
            childrenName,
            response => Assert.IsNull(response.Body?.ContinuationToken),
            response => Assert.IsNull(response.ContinuationToken));
    }

    [Then("the tenant called {string} should have the properties")]
    public void ThenTheTenantCalledShouldHaveTheProperties(string tenantName, DataTable dataTable)
    {
        IReadOnlyDictionary<string, object> actualProperties = ReadOnlyDictionary<string, object>.Empty;

        this.ProcessTenantResponseBasedOnType(
            tenantName,
            response => actualProperties = response.Body.Properties?.AsDictionaryRecursive() ?? throw new InvalidOperationException($"The tenant {tenantName} does not have any properties."),
            response => actualProperties = response.Properties.AsDictionaryRecursive());

        IEnumerable<(string Key, string Value, string Type)> expectedProperties = dataTable.CreateSet<(string Key, string Value, string Type)>();

        foreach ((string key, string value, string type) in expectedProperties)
        {
            Assert.IsTrue(
                actualProperties.ContainsKey(key),
                $"Property '{key}' not found in properties. Available keys: {string.Join(", ", actualProperties.Keys)}");

            if (type == "integer")
            {
                Assert.AreEqual(int.Parse(value), actualProperties[key]);
            }
            else if (type == "datetimeoffset")
            {
                // The value comes back as a complex object containing the serialized DateTimeOffset
                object actualValue = actualProperties[key];
                if (actualValue is Dictionary<string, object?> dict)
                {
                    Assert.IsTrue(dict.ContainsKey("dateTimeOffset"), "DateTimeOffset object should contain 'dateTimeOffset' property");
                    string serializedValue = dict["dateTimeOffset"]?.ToString()!;
                    Assert.AreEqual(DateTimeOffset.Parse(value), DateTimeOffset.Parse(serializedValue));
                }
                else
                {
                    // Fallback: try direct comparison
                    Assert.AreEqual(DateTimeOffset.Parse(value), actualValue);
                }
            }
            else
            {
                Assert.AreEqual(value, actualProperties[key]);
            }
        }
    }

    public static (IDictionary<string, object>? PropertiesToAddOrUpdate, IEnumerable<string>? PropertiesToRemove) DataTableToUpdateTenantJsonPatchEntry(DataTable dataTable)
    {
        IDictionary<string, object> updates = new Dictionary<string, object>();
        IList<string> removals = [];

        foreach ((string key, string value, string type, string action) in dataTable.CreateSet<(string Key, string Value, string Type, string Action)>())
        {
            if (action == "Remove")
            {
                removals.Add(key);
            }
            else
            {
                if (type == "integer")
                {
                    updates.Add(key, int.Parse(value));
                }
                else if (type == "datetimeoffset")
                {
                    updates.Add(key, DateTimeOffset.Parse(value));
                }
                else
                {
                    updates.Add(key, value);
                }
            }
        }

        return (updates, removals);
    }

    private void ProcessTenantResponseBasedOnType(
        string tenantName,
        Action<ApiResponse<TenantResource>> actionWhenTenancyClientResponse,
        Action<ITenant> actionWhenClientTenantProviderResponse)
    {
        this.ProcessResponsesBasedOnType<ApiResponse<TenantResource>, ITenant>(
            [tenantName],
            responses => actionWhenTenancyClientResponse(responses[0]),
            responses => actionWhenClientTenantProviderResponse(responses[0]));
    }

    private void ProcessTenantResponsesBasedOnType(
        string[] tenantNames,
        Action<ApiResponse<TenantResource>[]> actionWhenTenancyClientResponse,
        Action<ITenant[]> actionWhenClientTenantProviderResponse)
    {
        this.ProcessResponsesBasedOnType<ApiResponse<TenantResource>, ITenant>(
            tenantNames,
            responses => actionWhenTenancyClientResponse(responses),
            responses => actionWhenClientTenantProviderResponse(responses));
    }

    private void ProcessGetChildrenResponseBasedOnType(
        string tenantName,
        Action<ApiResponse<ChildTenantsResource>> actionWhenTenancyClientResponse,
        Action<TenantCollectionResult> actionWhenClientTenantProviderResponse)
    {
        this.ProcessResponsesBasedOnType<ApiResponse<ChildTenantsResource>, TenantCollectionResult>(
            [tenantName],
            responses => actionWhenTenancyClientResponse(responses[0]),
            responses => actionWhenClientTenantProviderResponse(responses[0]));
    }

    private async Task ProcessTenantResponseBasedOnTypeAsync(
    string tenantName,
    Func<ApiResponse<TenantResource>, Task> actionWhenTenancyClientResponse,
    Func<ITenant, Task> actionWhenClientTenantProviderResponse)
    {
        await this.ProcessTenantResponsesBasedOnTypeAsync(
            [tenantName],
            responses => actionWhenTenancyClientResponse(responses[0]),
            responses => actionWhenClientTenantProviderResponse(responses[0])).ConfigureAwait(false);
    }

    private void ProcessResponsesBasedOnType<T1, T2>(
        string[] tenantNames,
        Action<T1[]> action1,
        Action<T2[]> action2)
    {
        RethrowLastExceptionIfPresent(this.ScenarioContext);

        IEnumerable<object> unknownResponses = [.. tenantNames.Select(x => this.ScenarioContext[x])];

        T1[] t1Responses = [.. unknownResponses.OfType<T1>()];
        T2[] t2Responses = [.. unknownResponses.OfType<T2>()];

        if (t1Responses.Length != 0 && t2Responses.Length != 0)
        {
            throw new InvalidOperationException("Tenant names include responses of multiple types.");
        }

        if (t1Responses.Length > 0)
        {
            action1(t1Responses);
        }
        else if (t2Responses.Length > 0)
        {
            action2(t2Responses);
        }
    }

    private async Task ProcessTenantResponsesBasedOnTypeAsync(
        string[] tenantNames,
        Func<ApiResponse<TenantResource>[], Task> actionWhenTenancyClientResponses,
        Func<ITenant[], Task> actionWhenClientTenantProviderResponses)
    {
        RethrowLastExceptionIfPresent(this.ScenarioContext);

        IEnumerable<object> unknownResponses = [.. tenantNames.Select(x => this.ScenarioContext[x])];

        ApiResponse<TenantResource>[] tenancyClientResponses = [.. unknownResponses.OfType<ApiResponse<TenantResource>>()];
        ITenant[] clientTenantProviderResponses = [.. unknownResponses.OfType<ITenant>()];

        if (tenancyClientResponses.Length != 0 && clientTenantProviderResponses.Length != 0)
        {
            throw new InvalidOperationException("Tenant names include tenant responses from both tenancy client and client tenant provider.");
        }

        if (tenancyClientResponses.Length > 0)
        {
            await actionWhenTenancyClientResponses(tenancyClientResponses).ConfigureAwait(false);
        }
        else if (clientTenantProviderResponses.Length > 0)
        {
            await actionWhenClientTenantProviderResponses(clientTenantProviderResponses).ConfigureAwait(false);
        }
    }
}