// <copyright file="TenancyClient.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Marain.Clients;
using Marain.Tenancy.Client.Requests;
using Marain.Tenancy.Client.Resources;

public class TenancyClient(HttpClient client, JsonSerializerOptions serializerOptions) : ClientBase(client, serializerOptions), ITenancyClient
{
    public Task<ApiResponse<TenantResource>> CreateChildTenantAsync(
        string parentTenantId,
        string tenantName,
        string? wellKnownChildTenantGuid = null,
        CancellationToken cancellationToken = default) => 
        this.CreateChildTenantByLinkAsync(
            $"/{parentTenantId}/marain/tenant",
            tenantName,
            wellKnownChildTenantGuid,
            cancellationToken);

    public async Task<ApiResponse<TenantResource>> CreateChildTenantByLinkAsync(string parentTenantLink, string tenantName, string? wellKnownChildTenantGuid = null, CancellationToken cancellationToken = default)
    {
        Uri uri = this.ConstructUri(parentTenantLink);
        CreateChildTenantRequest body = new() { TenantName = tenantName, WellKnownChildTenantGuid = wellKnownChildTenantGuid };
        HttpRequestMessage request = this.BuildRequest(HttpMethod.Post, uri, body);
        HttpResponseMessage response = await this.SendRequestAndThrowOnFailureAsync(request, cancellationToken).ConfigureAwait(false);
        return await this.BuildApiResponseAsync<TenantResource>(response, cancellationToken).ConfigureAwait(false);
    }

    public Task<ApiResponse> DeleteChildTenantAsync(string parentTenantId, string childTenantId, CancellationToken cancellationToken = default) =>
        this.DeleteChildTenantByLinkAsync($"/{parentTenantId}/marain/tenant/children/{childTenantId}", cancellationToken);

    public async Task<ApiResponse> DeleteChildTenantByLinkAsync(string deleteTenantLink, CancellationToken cancellationToken = default)
    {
        Uri uri = this.ConstructUri(deleteTenantLink);
        HttpRequestMessage request = this.BuildRequest(HttpMethod.Delete, uri);
        HttpResponseMessage response = await this.SendRequestAndThrowOnFailureAsync(request, cancellationToken).ConfigureAwait(false);
        return new ApiResponse(response.StatusCode, MapHttpResponseHeadersToDictionary(response.Headers));
    }

    public Task<ApiResponse<ChildTenantsResource>> GetChildrenAsync(
        string tenantId,
        string? continuationToken = null,
        int? maxItems = null, CancellationToken cancellationToken = default)
    {
        List<(string Key, string Value)> parameters = [];
        if (!string.IsNullOrWhiteSpace(continuationToken))
        {
            parameters.Add(("continuationToken", continuationToken));
        }

        if (maxItems.HasValue)
        {
            parameters.Add(("maxItems", maxItems.Value.ToString()));
        }

        Uri uri = this.ConstructUri($"/{tenantId}/marain/tenant/children", [.. parameters]);

        return this.GetPathAsync<ChildTenantsResource>(uri, null, cancellationToken);
    }

    public Task<ApiResponse<ChildTenantsResource>> GetChildrenByLinkAsync(string childTenantsLink, CancellationToken cancellationToken = default) =>
        this.GetPathAsync<ChildTenantsResource>(childTenantsLink, null, cancellationToken);

    public Task<ApiResponse<TenantResource>> GetTenantAsync(string tenantId, string? etag = null, CancellationToken cancellationToken = default) =>
        this.GetTenantByLinkAsync($"/{tenantId}/marain/tenant", etag, cancellationToken);
    
    public Task<ApiResponse<TenantResource>> GetTenantByLinkAsync(string tenantLink, string? etag = null, CancellationToken cancellationToken = default) =>
        this.GetPathAsync<TenantResource>(
            tenantLink,
            request => request.Headers.Add("If-Not-Modified", etag),
            cancellationToken);

    public Task<ApiResponse<TenantResource>> UpdateTenantAsync(string tenantId, string? newName, IEnumerable<KeyValuePair<string, object>>? propertiesToAddOrUpdate, IEnumerable<string>? propertiesToRemove, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }
}
