// <copyright file="MinimalApiTestableTenancyService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.MultiHost;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json.Linq;

/// <summary>
/// Implementation of ITestableTenancyService that uses WebApplicationFactory to test the MinimalApi.
/// </summary>
internal class MinimalApiTestableTenancyService : ITestableTenancyService, IDisposable
{
    private readonly WebApplicationFactory<Marain.Tenancy.MinimalApi.Program> factory;
    private readonly HttpClient httpClient;
    private HttpResponseMessage? response;
    private string? responseContent;
    private JObject? parsedResponse;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinimalApiTestableTenancyService"/> class.
    /// </summary>
    /// <param name="factory">The web application factory for creating test clients.</param>
    public MinimalApiTestableTenancyService(WebApplicationFactory<Marain.Tenancy.MinimalApi.Program> factory)
    {
        this.factory = factory;
        this.httpClient = factory.CreateClient();
    }

    /// <inheritdoc/>
    public async Task<TenancyResponse> CreateTenantAsync(string parentId, string name)
    {
        var requestBody = new { TenantName = name };
        await this.SendPostRequest($"/{parentId}/marain/tenant", requestBody);
        return this.MakeResponse();
    }

    /// <inheritdoc/>
    public async Task<TenancyResponse> GetSwaggerAsync()
    {
        await this.SendGetRequest("/swagger/v1/swagger.json");
        return this.MakeResponse();
    }

    /// <inheritdoc/>
    public async Task<TenancyResponse> GetTenantAsync(string tenantId, string? etag)
    {
        await this.SendGetRequest($"/{tenantId}/marain/tenant", etag);
        return this.MakeResponse();
    }

    /// <inheritdoc/>
    public async Task<TenancyResponse> GetTenantByLocationAsync(string location)
    {
        await this.SendGetRequest(location);
        return this.MakeResponse();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.httpClient?.Dispose();
        this.response?.Dispose();
    }

    private TenancyResponse MakeResponse()
    {
        return new TenancyResponse
        {
            LocationHeader = this.response!.Headers.Location?.ToString(),
            EtagHeader = this.response.Headers.ETag?.Tag,
            IsSuccessStatusCode = this.response.IsSuccessStatusCode,
            StatusCode = this.response.StatusCode,
            CacheControlHeader = this.response.Headers.CacheControl?.ToString(),
            BodyJson = this.parsedResponse,
        };
    }

    private async Task SendGetRequest(string path, string? etag = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (!string.IsNullOrEmpty(etag))
        {
            request.Headers.Add("If-None-Match", etag);
        }

        this.response = await this.httpClient.SendAsync(request);
        this.responseContent = await this.response.Content.ReadAsStringAsync();

        if (this.response.IsSuccessStatusCode && !string.IsNullOrEmpty(this.responseContent))
        {
            try
            {
                this.parsedResponse = JObject.Parse(this.responseContent);
            }
            catch
            {
                // If JSON parsing fails, leave parsedResponse as null
                this.parsedResponse = null;
            }
        }
    }

    private async Task SendPostRequest(string path, object? data)
    {
        HttpContent? content = null;

        if (data is not null)
        {
            string requestJson = System.Text.Json.JsonSerializer.Serialize(data);
            content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
        }

        this.response = await this.httpClient.PostAsync(path, content);
        this.responseContent = await this.response.Content.ReadAsStringAsync();

        if (this.response.IsSuccessStatusCode && !string.IsNullOrEmpty(this.responseContent))
        {
            try
            {
                this.parsedResponse = JObject.Parse(this.responseContent);
            }
            catch
            {
                // If JSON parsing fails, leave parsedResponse as null
                this.parsedResponse = null;
            }
        }
    }
}