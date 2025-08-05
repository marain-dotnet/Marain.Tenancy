// <copyright file="ClientTestableTenancyService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Helpers;

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Marain.Tenancy.Specs.Bindings;

internal class ClientTestableTenancyService
{
    private static readonly HttpClient HttpClient = new();
    private readonly string tenancyApiBaseUriText;
    private HttpResponseMessage? response;
    private string? responseContent;
    private JsonObject? parsedResponse;

    public ClientTestableTenancyService(string tenancyApiBaseUriText)
    {
        this.tenancyApiBaseUriText = tenancyApiBaseUriText;
    }

    public async Task<TenancyResponse> CreateTenantAsync(string parentId, string name)
    {
        await this.SendPostRequest(new(this.tenancyApiBaseUriText), $"/{parentId}/marain/tenant/children?tenantName={Uri.EscapeDataString(name)}", null);

        return this.MakeResponse();
    }

    public async Task<TenancyResponse> GetSwaggerAsync()
    {
        await this.SendGetRequest(new(this.tenancyApiBaseUriText), "/swagger");
        return this.MakeResponse();
    }

    public async Task<TenancyResponse> GetTenantAsync(string tenantId, string? etag)
    {
        await this.SendGetRequest(new(this.tenancyApiBaseUriText), $"/{tenantId}/marain/tenant", etag);
        return this.MakeResponse();
    }

    public async Task<TenancyResponse> GetTenantByLocationAsync(string location)
    {
        await this.SendGetRequest(new(this.tenancyApiBaseUriText), location);
        return this.MakeResponse();
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
            this.parsedResponse = JsonNode.Parse(this.responseContent)?.AsObject();
        }
    }

    private async Task SendPostRequest(Uri baseUri, string path, object? data)
    {
        HttpContent? content = null;

        if (data is not null)
        {
            string requestJson = JsonSerializer.Serialize(data);
            content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        }

        this.response = await HttpClient.PostAsync(new Uri(baseUri, path), content).ConfigureAwait(false);
        this.responseContent = await this.response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
}