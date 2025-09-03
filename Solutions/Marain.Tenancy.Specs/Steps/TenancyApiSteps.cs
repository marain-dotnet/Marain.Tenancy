// <copyright file="TenancyApiSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Steps;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Corvus.Json.Serialization;
using Corvus.Testing.ReqnRoll;
using Marain.Tenancy.Specs.Bindings;
using Marain.Tenancy.Specs.Helpers;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Reqnroll;

[Binding]
public class TenancyApiSteps : Steps
{
    private readonly Dictionary<string, string> namedIds = new();
    private TenancyResponse? tenancyResponse;

    private TenancyResponse Response => this.tenancyResponse ?? throw new InvalidOperationException("No response available");

    [When(@"I request the tenancy service \/swagger endpoint")]
    public async Task WhenIRequestTheTenancyServiceEndpoint()
    {
        await this.GetSwaggerAsync();
    }

    [Given("I have requested the tenant with the ID called '([^']*)'")]
    public async Task GivenIHaveRequestedTheTenantWithTheIDCalledAsync(string idName)
    {
        await this.WhenIRequestTheTenantWithIdFromTheAPI(this.namedIds[idName]);
    }

    [When("I request the tenant with Id '(.*)' from the API")]
    public async Task WhenIRequestTheTenantWithIdFromTheAPI(string tenantId)
    {
        await this.GetTenantAsync(tenantId);
    }

    [When("I request the tenant using the Location from the previous response")]
    public async Task WhenIRequestTheTenantUsingTheLocationFromThePreviousResponse()
    {
        await this.GetTenantByLocationAsync(this.Response.LocationHeader ?? throw new InvalidOperationException("This test step should only be executed if an earlier step produced a response with a location header."));
    }

    [When("I request the tenant using the ID called '([^']*)' and the Etag from the previous response")]
    public async Task WhenIRequestTheTenantUsingTheIDCalledAndTheEtagFromThePreviousResponseAsync(string idName)
    {
        await this.GetTenantAsync(this.namedIds[idName], this.Response.EtagHeader);
    }

    [Given("I store the id from the response Location header as '([^']*)'")]
    public void GivenIStoreTheIdFromTheResponseLocationHeaderAs(string idName)
    {
        this.namedIds.Add(idName, this.GetTenantIdFromLocationHeader());
    }

    [Given("I store the value of the response Location header as '(.*)'")]
    public void GivenIStoreTheValueOfTheResponseLocationHeaderAs(string name)
    {
        this.ScenarioContext.Set(this.Response.LocationHeader, name);
    }

    [Given("I have requested the tenant using the path called '(.*)'")]
    public async Task GivenIHaveRequestedTheTenantUsingThePathCalled(string name)
    {
        string path = this.ScenarioContext.Get<string>(name);
        await this.SendGetRequest(path);
    }

    [When("I request the tenant using the path called '(.*)' and the Etag from the previous response")]
    public Task WhenIRequestTheTenantUsingThePathCalledAndTheEtagFromThePreviousResponse(string name)
    {
        string path = this.ScenarioContext.Get<string>(name);

        return this.SendGetRequest(
            path,
            this.Response.EtagHeader ?? throw new InvalidOperationException("ETag not available from previous response"));
    }

    [Given("I have used the API to create a new tenant")]
    [When("I use the API to create a new tenant")]
    public async Task WhenIUseTheAPIToCreateANewTenant(Table table)
    {
        string parentId = table.Rows[0]["ParentTenantId"];
        string name = table.Rows[0]["Name"];

        await this.CreateTenantAsync(parentId, name);

        if (this.Response.IsSuccessStatusCode && this.Response.LocationHeader is not null)
        {
            string id = this.GetTenantIdFromLocationHeader();
            TestTenantCleanup.AddTenantToDelete(parentId, id);
        }
    }

    [Then("I receive a '(.*)' response")]
    [Then("I receive an '(.*)' response")]
    public void ThenIReceiveAResponse(HttpStatusCode expectedStatusCode)
    {
        // If present, we'll use the response content as a message in case of failure
        Assert.AreEqual(expectedStatusCode, this.Response.StatusCode, this.Response.BodyRaw);
    }

    [Then("the response should contain a Location header")]
    public void ThenTheResponseShouldContainALocationHeader()
    {
        Assert.IsNotNull(this.Response.LocationHeader);
    }

    [Then("the response should contain an Etag header")]
    public void ThenTheResponseShouldContainAnEtagHeader()
    {
        Assert.IsNotNull(this.Response.EtagHeader);
    }

    [Then("the response should not contain an Etag header")]
    public void ThenTheResponseShouldNotContainAnEtagHeader()
    {
        Assert.IsNull(this.Response.EtagHeader);
    }

    [Then("the response should contain a Cache-Control header with value '(.*)'")]
    public void ThenTheResponseShouldContainAHeaderWithValue(string expectedValue)
    {
        Assert.AreEqual(expectedValue, this.Response.CacheControlHeader);
    }

    [Then("the response content should contain a {string} link")]
    public void ThenTheResponseShouldContainALink(string linkName)
    {
        Assert.DoesNotThrow(() => this.GetPropertyNodeFromBodyJsonByPath($"_links.{linkName}"));
    }

    [Then("the response content should contain a {string} link with href with path {string}")]
    public void ThenTheResponseShouldContainALinkWithHref(string linkName, string expectedHrefPath)
    {
        JsonNode targetProperty = this.GetPropertyNodeFromBodyJsonByPath($"_links.{linkName}.href");
        string? actualValue = targetProperty.GetValue<string>();

        Assert.IsTrue(actualValue?.EndsWith(expectedHrefPath));
    }

    [Then("the response content should have a string property called '(.*)' with value '(.*)'")]
    public void ThenTheResponseObjectShouldHaveAStringPropertyCalledWithValue(string propertyPath, string expectedValue)
    {
        JsonNode targetProperty = this.GetPropertyNodeFromBodyJsonByPath(propertyPath);
        string? actualValue = targetProperty.GetValue<string>();
        Assert.AreEqual(expectedValue, actualValue, $"Expected value of property '{propertyPath}' was '{expectedValue}', but actual value was '{actualValue}'");
    }

    private JsonNode GetPropertyNodeFromBodyJsonByPath(string propertyPath)
    {
        string[] pathElements = propertyPath.Split(".", StringSplitOptions.RemoveEmptyEntries);

        JsonNode currentObject = this.Response.BodyJson ?? throw new InvalidOperationException("There is no BodyJson");

        foreach (string pathElement in pathElements)
        {
            currentObject = currentObject[pathElement] ?? throw new InvalidOperationException($"Property named '{pathElement}' not found.");
        }

        return currentObject;
    }

    private string GetTenantIdFromLocationHeader()
    {
        string location = this.Response.LocationHeader ?? throw new InvalidOperationException("No location header");
        return location[1..location.IndexOf('/', 1)];
    }

    private async Task CreateTenantAsync(string parentId, string name)
    {
        var requestBody = new { TenantName = name };
        await this.SendPostRequest($"/{parentId}/marain/tenant", requestBody);
    }

    private async Task GetSwaggerAsync()
    {
        await this.SendGetRequest("/swagger");
    }

    private async Task GetTenantAsync(string tenantId, string? etag = null)
    {
        await this.SendGetRequest($"/{tenantId}/marain/tenant", etag);
    }

    private async Task GetTenantByLocationAsync(string location)
    {
        await this.SendGetRequest(location);
    }

    private async Task SetResponseAsync(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();

        this.tenancyResponse = new TenancyResponse
        {
            LocationHeader = response!.Headers.Location?.ToString(),
            EtagHeader = response.Headers.ETag?.Tag,
            IsSuccessStatusCode = response.IsSuccessStatusCode,
            StatusCode = response.StatusCode,
            CacheControlHeader = response.Headers.CacheControl?.ToString(),
            BodyRaw = body,
            BodyJson = ParseResponseBody(response, body),
        };
    }

    private static JsonObject? ParseResponseBody(HttpResponseMessage response, string responseContent)
    {
        if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(responseContent))
        {
            try
            {
                return JsonNode.Parse(responseContent)?.AsObject();
            }
            catch
            {
                // If JSON parsing fails, leave parsedResponse as null
            }
        }

        return null;
    }

    private async Task SendGetRequest(string path, string? etag = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (!string.IsNullOrEmpty(etag))
        {
            request.Headers.Add("If-None-Match", etag);
        }

        HttpResponseMessage response = await ApiWebApplicationFactory.Current.Client.SendAsync(request);
        await this.SetResponseAsync(response);
    }

    private async Task SendPostRequest(string path, object? data)
    {
        IServiceProvider serviceProvider = ContainerBindings.GetServiceProvider(this.FeatureContext);
        IJsonSerializerOptionsProvider serializationOptionsProvider = serviceProvider.GetRequiredService<IJsonSerializerOptionsProvider>();

        HttpContent? content = null;

        if (data is not null)
        {
            string requestJson = JsonSerializer.Serialize(data, serializationOptionsProvider.Instance);

            TestContext.WriteLine($"Serialized request body for POST {path}:");
            TestContext.WriteLine(requestJson);

            content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response = await ApiWebApplicationFactory.Current.Client.PostAsync(path, content);
        await this.SetResponseAsync(response);
    }

    private class TenancyResponse
    {
        public string? LocationHeader { get; set; }

        public string? EtagHeader { get; set; }

        public bool IsSuccessStatusCode { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public string? CacheControlHeader { get; set; }

        public string? BodyRaw { get; set; }

        public JsonObject? BodyJson { get; set; }
    }
}