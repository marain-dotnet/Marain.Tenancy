// <copyright file="TenancyApiSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Steps
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Corvus.Extensions.Json;
    using Corvus.Testing.SpecFlow;

    using Marain.Tenancy.Specs.Integration.Bindings;
    using Marain.Tenancy.Specs.MultiHost;
    using Microsoft.Extensions.DependencyInjection;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using NUnit.Framework;

    using TechTalk.SpecFlow;

    [Binding]
    public class TenancyApiSteps : Steps
    {        
        private static readonly HttpClient HttpClient = new ();
        private readonly TestTenantCleanup testTenantCleanup;
        private readonly ITestableTenancyService serviceWrapper;
        private HttpResponseMessage? response;
        private string? responseContent;
        private TenancyResponse? tenancyResponse;

        public TenancyApiSteps(
            TestTenantCleanup testTenantCleanup, 
            ITestableTenancyService serviceWrapper)
        {
            this.testTenantCleanup = testTenantCleanup;
            this.serviceWrapper = serviceWrapper;
        }

        private HttpResponseMessage Response => this.response ?? throw new InvalidOperationException("No response available");

        [When("I request the tenancy service endpoint '(.*)'")]
        public Task WhenIRequestTheTenancyServiceEndpoint(string endpointPath)
        {
            return this.SendGetRequest(FunctionBindings.TenancyApiBaseUri, endpointPath);
        }

        [When("I request the tenant with Id '(.*)' from the API")]
        public async Task WhenIRequestTheTenantWithIdFromTheAPI(string tenantId)
        {
            this.tenancyResponse = await this.serviceWrapper.GetTenantAsync(tenantId);
            // return this.SendGetRequest(FunctionBindings.TenancyApiBaseUri, $"/{tenantId}/marain/tenant");
        }

        [When("I request the tenant using the Location from the previous response")]
        public Task WhenIRequestTheTenantUsingTheLocationFromThePreviousResponse()
        {
            return this.SendGetRequest(
                FunctionBindings.TenancyApiBaseUri,
                (this.Response.Headers.Location ?? throw new InvalidOperationException("Location header unavailable")).ToString());
        }

        [Given("I store the value of the response Location header as '(.*)'")]
        public void GivenIStoreTheValueOfTheResponseLocationHeaderAs(string name)
        {
            this.ScenarioContext.Set(this.Response.Headers.Location?.ToString(), name);
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
                (this.Response.Headers.ETag ?? throw new InvalidOperationException("ETag not available from previous response")).Tag);
        }

        [Given("I have used the API to create a new tenant")]
        [When("I use the API to create a new tenant")]
        public async Task WhenIUseTheAPIToCreateANewTenant(Table table)
        {
            string parentId = table.Rows[0]["ParentTenantId"];
            string name = table.Rows[0]["Name"];

            await this.SendPostRequest(FunctionBindings.TenancyApiBaseUri, $"/{parentId}/marain/tenant/children?tenantName={Uri.EscapeDataString(name)}", null);
            if (this.Response.IsSuccessStatusCode && this.Response.Headers.Location is not null)
            {
                string location = this.Response.Headers.Location.ToString();
                string id = location[1..location.IndexOf('/', 1)];
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

        [Then("the response should contain a '(.*)' header")]
        [Then("the response should contain an '(.*)' header")]
        public void ThenTheResponseShouldContainAHeader(string headerName)
        {
            Assert.IsTrue(this.Response.Headers.Contains(headerName));
        }

        [Then("the response should not contain a '(.*)' header")]
        [Then("the response should not contain an '(.*)' header")]
        public void ThenTheResponseShouldNotContainAHeader(string headerName)
        {
            Assert.IsFalse(this.Response.Headers.Contains(headerName));
        }

        [Then("the response should contain a '(.*)' header with value '(.*)'")]
        [Then("the response should contain an '(.*)' header with value '(.*)'")]
        public void ThenTheResponseShouldContainAHeaderWithValue(string headerName, string expectedValue)
        {
            Assert.IsTrue(this.Response.Headers.Contains(headerName));

            string value = this.Response.Headers.GetValues(headerName).First();
            Assert.AreEqual(expectedValue, value);
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

        private async Task SendPostRequest(Uri baseUri, string path, object? data)
        {
            HttpContent? content = null;

            if (data is not null)
            {
                IServiceProvider serviceProvider = ContainerBindings.GetServiceProvider(this.FeatureContext);
                IJsonSerializerSettingsProvider serializerSettingsProvider = serviceProvider.GetRequiredService<IJsonSerializerSettingsProvider>();
                string requestJson = JsonConvert.SerializeObject(data, serializerSettingsProvider.Instance);
                content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            }

            this.response = await HttpClient.PostAsync(new Uri(baseUri, path), content).ConfigureAwait(false);
            this.responseContent = await this.response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}
