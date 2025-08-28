// <copyright file="CommonSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Steps;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Corvus.Json;
using Corvus.Tenancy;
using Corvus.Testing.ReqnRoll;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Models;
using Marain.Tenancy.Specs.Bindings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;
using NUnit.Framework;
using Reqnroll;

[Binding]
public class CommonSteps : Steps
{
    private const string LastExceptionKey = "LastException";

    private ITenantStore? store;
    private TenancyApiClient apiClient;

    public CommonSteps(FeatureContext featureContext)
    {
        this.store = ContainerBindings.GetServiceProvider(featureContext).GetService<ITenantStore>();
        this.apiClient = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<TenancyApiClient>();
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

    [Then("it should throw an ApiException with Response Status Code {int}")]
    public void ThenTheApiExceptionShouldHaveStatusCode(int expectedStatusCode)
    {
        Exception? lastException = GetLastException(this.ScenarioContext);
        Assert.IsInstanceOf<ApiException>(lastException);
        var ex = (ApiException)lastException!;
        Assert.AreEqual(expectedStatusCode, ex.ResponseStatusCode);
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
                tenant.Headers.TryGetValue("ETag", out IEnumerable<string>? etagValues);
                string? etag = etagValues?.FirstOrDefault();

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

                CreateChildTenantRequest request = new() { TenantName = childName };
                HeadersInspectionHandlerOption headersInspectionhandler = new() { InspectResponseHeaders = true };
                TenantResponse? response = await this.apiClient[parentTenant.Body!.Id].Marain.Tenant.PostAsync(request, config => config.Options.Add(headersInspectionhandler)).ConfigureAwait(false);

                TestTenantCleanup.AddTenantToDelete(parentTenant.Body.Id, response?.Id);

                this.ScenarioContext.Set(new ApiResponseWithHeaders<TenantResponse>(response, headersInspectionhandler.ResponseHeaders), childName);
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
        Dictionary<string, object> actualProperties = [];

        this.ProcessTenantResponseBasedOnType(
            tenantName,
            response => actualProperties = response.Body?.Properties?.AdditionalData.ToDictionary() ?? throw new InvalidOperationException($"The tenant {tenantName} does not have any properties."),
            response => actualProperties = response.Properties.AsDictionary().ToDictionary());

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

    private void ProcessTenantResponseBasedOnType(
        string tenantName,
        Action<ApiResponseWithHeaders<TenantResponse>> actionWhenTenancyClientResponse,
        Action<ITenant> actionWhenClientTenantProviderResponse)
    {
        this.ProcessResponsesBasedOnType<ApiResponseWithHeaders<TenantResponse>, ITenant>(
            [tenantName],
            responses => actionWhenTenancyClientResponse(responses[0]),
            responses => actionWhenClientTenantProviderResponse(responses[0]));
    }

    private void ProcessTenantResponsesBasedOnType(
        string[] tenantNames,
        Action<ApiResponseWithHeaders<TenantResponse>[]> actionWhenTenancyClientResponse,
        Action<ITenant[]> actionWhenClientTenantProviderResponse)
    {
        this.ProcessResponsesBasedOnType<ApiResponseWithHeaders<TenantResponse>, ITenant>(
            tenantNames,
            responses => actionWhenTenancyClientResponse(responses),
            responses => actionWhenClientTenantProviderResponse(responses));
    }

    private void ProcessGetChildrenResponseBasedOnType(
        string tenantName,
        Action<ApiResponseWithHeaders<ChildTenantsResponse>> actionWhenTenancyClientResponse,
        Action<TenantCollectionResult> actionWhenClientTenantProviderResponse)
    {
        this.ProcessResponsesBasedOnType<ApiResponseWithHeaders<ChildTenantsResponse>, TenantCollectionResult>(
            [tenantName],
            responses => actionWhenTenancyClientResponse(responses[0]),
            responses => actionWhenClientTenantProviderResponse(responses[0]));
    }

    private async Task ProcessTenantResponseBasedOnTypeAsync(
    string tenantName,
    Func<ApiResponseWithHeaders<TenantResponse>, Task> actionWhenTenancyClientResponse,
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
        Func<ApiResponseWithHeaders<TenantResponse>[], Task> actionWhenTenancyClientResponses,
        Func<ITenant[], Task> actionWhenClientTenantProviderResponses)
    {
        RethrowLastExceptionIfPresent(this.ScenarioContext);

        IEnumerable<object> unknownResponses = [.. tenantNames.Select(x => this.ScenarioContext[x])];

        ApiResponseWithHeaders<TenantResponse>[] tenancyClientResponses = [.. unknownResponses.OfType<ApiResponseWithHeaders<TenantResponse>>()];
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