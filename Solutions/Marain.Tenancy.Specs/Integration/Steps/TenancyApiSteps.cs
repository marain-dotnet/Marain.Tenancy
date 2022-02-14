// <copyright file="TenancyApiSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Marain.Tenancy.Specs.Integration.Bindings;
    using Marain.Tenancy.Specs.MultiHost;

    using Newtonsoft.Json.Linq;

    using NUnit.Framework;

    using TechTalk.SpecFlow;

    [Binding]
    public class TenancyApiSteps : Steps
    {
        private static readonly HttpClient HttpClient = new();
        private readonly Dictionary<string, string> namedIds = new();
        private readonly TestTenantCleanup testTenantCleanup;
        private readonly ITestableTenancyService serviceWrapper;
        private readonly JsonSteps jsonSteps;
        private HttpResponseMessage? response;
        private string? responseContent;
        private TenancyResponse? tenancyResponse;

        public TenancyApiSteps(
            TestTenantCleanup testTenantCleanup,
            ITestableTenancyService serviceWrapper,
            JsonSteps jsonSteps)
        {
            this.testTenantCleanup = testTenantCleanup;
            this.serviceWrapper = serviceWrapper;
            this.jsonSteps = jsonSteps;
        }

        private TenancyResponse Response => this.tenancyResponse ?? throw new InvalidOperationException("No response available");

        [When("I request the tenancy service endpoint '/swagger'")]
        public async Task WhenIRequestTheTenancyServiceEndpoint()
        {
            this.tenancyResponse = await this.serviceWrapper.GetSwaggerAsync();
        }

        [Given("I have requested the tenant with the ID called '([^']*)'")]
        public async Task GivenIHaveRequestedTheTenantWithTheIDCalledAsync(string idName)
        {
            await this.WhenIRequestTheTenantWithIdFromTheAPI(this.namedIds[idName]);
        }

        [When("I request the tenant with Id '(.*)' from the API")]
        public async Task WhenIRequestTheTenantWithIdFromTheAPI(string tenantId)
        {
            this.tenancyResponse = await this.serviceWrapper.GetTenantAsync(tenantId);
            this.jsonSteps.Json = this.tenancyResponse.BodyJson;
        }

        [When("I request the tenant using the Location from the previous response")]
        public async Task WhenIRequestTheTenantUsingTheLocationFromThePreviousResponse()
        {
            this.tenancyResponse = await this.serviceWrapper.GetTenantByLocationAsync(this.Response.LocationHeader ?? throw new InvalidOperationException("This test step should only be executed if an earlier step produced a response with a location header."));
            this.jsonSteps.Json = this.tenancyResponse.BodyJson;
        }

        [When("I request the tenant using the ID called '([^']*)' and the Etag from the previous response")]
        public async Task WhenIRequestTheTenantUsingTheIDCalledAndTheEtagFromThePreviousResponseAsync(string idName)
        {
            this.tenancyResponse = await this.serviceWrapper.GetTenantAsync(this.namedIds[idName], this.Response.EtagHeader);
            this.jsonSteps.Json = this.tenancyResponse.BodyJson;
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
        public Task GivenIHaveRequestedTheTenantUsingThePathCalled(string name)
        {
            string path = this.ScenarioContext.Get<string>(name);
            return this.SendGetRequest(FunctionBindings.TenancyApiBaseUri, path);
        }

        [When("I request the tenant using the path called '(.*)' and the Etag from the previous response")]
        public Task WhenIRequestTheTenantUsingThePathCalledAndTheEtagFromThePreviousResponse(string name)
        {
            string path = this.ScenarioContext.Get<string>(name);

            return this.SendGetRequest(
                FunctionBindings.TenancyApiBaseUri,
                path,
                this.Response.EtagHeader ?? throw new InvalidOperationException("ETag not available from previous response"));
        }

        [Given("I have used the API to create a new tenant")]
        [When("I use the API to create a new tenant")]
        public async Task WhenIUseTheAPIToCreateANewTenant(Table table)
        {
            string parentId = table.Rows[0]["ParentTenantId"];
            string name = table.Rows[0]["Name"];

            this.tenancyResponse = await this.serviceWrapper.CreateTenantAsync(parentId, name);
            if (this.Response.IsSuccessStatusCode && this.Response.LocationHeader is not null)
            {
                string id = this.GetTenantIdFromLocationHeader();
                this.testTenantCleanup.AddTenantToDelete(parentId, id);
            }
        }

        [Then("I receive a '(.*)' response")]
        [Then("I receive an '(.*)' response")]
        public void ThenIReceiveAResponse(HttpStatusCode expectedStatusCode)
        {
            // If present, we'll use the response content as a message in case of failure
            Assert.AreEqual(expectedStatusCode, this.Response.StatusCode, this.responseContent);
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

        private string GetTenantIdFromLocationHeader()
        {
            string location = this.Response.LocationHeader ?? throw new InvalidOperationException("No location header");
            return location[1..location.IndexOf('/', 1)];
        }

        private async Task SendGetRequest(Uri baseUri, string path, string? etag = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, path));

            if (!string.IsNullOrEmpty(etag))
            {
                request.Headers.Add("If-None-Match", etag);
            }

            this.response = await HttpClient.SendAsync(request).ConfigureAwait(false);
            this.responseContent = await this.response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (this.response.IsSuccessStatusCode && !string.IsNullOrEmpty(this.responseContent))
            {
                var parsedResponse = JObject.Parse(this.responseContent);
                this.ScenarioContext.Set(parsedResponse);
            }
        }
    }
}