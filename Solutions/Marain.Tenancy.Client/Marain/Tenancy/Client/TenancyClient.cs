// <copyright file="TenancyClient.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Marain.Clients;
using Marain.Tenancy.Client.Requests;
using Marain.Tenancy.Client.Resources;

/// <summary>
/// Implementation of the Tenancy API client.
/// </summary>
/// <param name="client">The HTTP client to use for API requests.</param>
/// <param name="serializerOptions">The JSON serializer options to use.</param>
public class TenancyClient(HttpClient client, JsonSerializerOptions serializerOptions) : ClientBase(client, serializerOptions), ITenancyClient
{
    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<ApiResponse<TenantResource>> CreateChildTenantByLinkAsync(string parentTenantLink, string tenantName, string? wellKnownChildTenantGuid = null, CancellationToken cancellationToken = default)
    {
        Uri uri = this.ConstructUri(parentTenantLink);
        CreateChildTenantRequest body = new() { TenantName = tenantName, WellKnownChildTenantGuid = wellKnownChildTenantGuid };
        HttpRequestMessage request = this.BuildRequest(HttpMethod.Post, uri, body);
        HttpResponseMessage response = await this.SendRequestAndThrowOnFailureAsync(request, cancellationToken).ConfigureAwait(false);
        return await this.BuildApiResponseAsync<TenantResource>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<ApiResponse> DeleteChildTenantAsync(string parentTenantId, string childTenantId, CancellationToken cancellationToken = default) =>
        this.DeleteChildTenantByLinkAsync($"/{parentTenantId}/marain/tenant/children/{childTenantId}", cancellationToken);

    /// <inheritdoc/>
    public async Task<ApiResponse> DeleteChildTenantByLinkAsync(string deleteTenantLink, CancellationToken cancellationToken = default)
    {
        Uri uri = this.ConstructUri(deleteTenantLink);
        HttpRequestMessage request = this.BuildRequest(HttpMethod.Delete, uri);
        HttpResponseMessage response = await this.SendRequestAndThrowOnFailureAsync(request, cancellationToken).ConfigureAwait(false);
        return new ApiResponse(response.StatusCode, MapHttpResponseHeadersToDictionary(response.Headers));
    }

    /// <inheritdoc/>
    public Task<ApiResponse<ChildTenantsResource>> GetChildrenAsync(
        string tenantId,
        string? continuationToken = null,
        int? maxItems = null,
        CancellationToken cancellationToken = default)
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

    /// <inheritdoc/>
    public Task<ApiResponse<ChildTenantsResource>> GetChildrenByLinkAsync(string childTenantsLink, CancellationToken cancellationToken = default) =>
        this.GetPathAsync<ChildTenantsResource>(childTenantsLink, null, cancellationToken);

    /// <inheritdoc/>
    public Task<ApiResponse<TenantResource>> GetTenantAsync(string tenantId, string? etag = null, CancellationToken cancellationToken = default) =>
        this.GetTenantByLinkAsync($"/{tenantId}/marain/tenant", etag, cancellationToken);

    /// <inheritdoc/>
    public Task<ApiResponse<TenantResource>> GetTenantByLinkAsync(string tenantLink, string? etag = null, CancellationToken cancellationToken = default) =>
        this.GetPathAsync<TenantResource>(
            tenantLink,
            request => request.Headers.Add("If-None-Match", etag),
            cancellationToken);

    /// <inheritdoc/>
    public async Task<ApiResponse<TenantResource>> UpdateTenantAsync(string tenantId, string? newName, IEnumerable<KeyValuePair<string, object>>? propertiesToAddOrUpdate, IEnumerable<string>? propertiesToRemove, CancellationToken cancellationToken = default)
    {
        IEnumerable<UpdateTenantPatchEntry> addOrUpdateEntries = propertiesToAddOrUpdate?.Select(x => UpdateTenantPatchEntry.CreateAddOrUpdateOperation(x.Key, x.Value)) ?? [];
        IEnumerable<UpdateTenantPatchEntry> removeEntries = propertiesToRemove?.Select(UpdateTenantPatchEntry.CreateDeleteOperation) ?? [];

        List<UpdateTenantPatchEntry> updateTenantPatchEntries =
            [
                ..addOrUpdateEntries,
                ..removeEntries
            ];

        if (!string.IsNullOrWhiteSpace(newName))
        {
            updateTenantPatchEntries.Add(UpdateTenantPatchEntry.CreateUpdateNameOperation(newName));
        }

        Uri uri = this.ConstructUri($"/{tenantId}/marain/tenant");
        HttpRequestMessage request = this.BuildRequest(HttpMethod.Patch, uri, updateTenantPatchEntries);
        HttpResponseMessage response = await this.SendRequestAndThrowOnFailureAsync(request, cancellationToken).ConfigureAwait(false);
        return await this.BuildApiResponseAsync<TenantResource>(response, cancellationToken).ConfigureAwait(false);
    }
}