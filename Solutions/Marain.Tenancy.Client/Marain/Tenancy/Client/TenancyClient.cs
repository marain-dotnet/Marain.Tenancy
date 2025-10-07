// <copyright file="TenancyClient.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Marain.Clients;
using Marain.Tenancy.Client.Requests;
using Marain.Tenancy.Client.Resources;
using Marain.Tenancy.Shared.Extensions;
using Marain.Tenancy.Shared.Telemetry;

/// <summary>
/// Implementation of the Tenancy API client.
/// </summary>
/// <param name="client">The HTTP client to use for API requests.</param>
/// <param name="serializerOptions">The JSON serializer options to use.</param>
public class TenancyClient(HttpClient client, JsonSerializerOptions serializerOptions) : ClientBase(client, serializerOptions), ITenancyClient
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ClientActivitySource);

    /// <inheritdoc/>
    public Task<ApiResponse<TenantResource>> CreateChildTenantAsync(
        string parentTenantId,
        string tenantName,
        string? wellKnownChildTenantGuid = null,
        CancellationToken cancellationToken = default) =>
        this.CreateChildTenantByLinkAsync(
            $"/{parentTenantId}/marain/tenant/children",
            tenantName,
            wellKnownChildTenantGuid,
            cancellationToken);

    /// <inheritdoc/>
    public async Task<ApiResponse<TenantResource>> CreateChildTenantByLinkAsync(string parentTenantLink, string tenantName, string? wellKnownChildTenantGuid = null, CancellationToken cancellationToken = default)
    {
        using Activity? activity = ActivitySource.StartActivity("client.create-child-tenant");
        activity?.SetTag("http.method", "POST");
        activity?.SetTag("http.url", parentTenantLink);
        activity?.SetTag("tenant.name", tenantName);
        activity?.SetTag("tenant.is_well_known", !string.IsNullOrEmpty(wellKnownChildTenantGuid));

        try
        {
            Uri uri = this.ConstructUri(parentTenantLink);
            CreateChildTenantRequest body = new() { TenantName = tenantName, WellKnownChildTenantGuid = wellKnownChildTenantGuid };
            HttpRequestMessage request = this.BuildRequest(HttpMethod.Post, uri, body);
            HttpResponseMessage response = await this.SendRequestAndThrowOnFailureAsync(request, cancellationToken).ConfigureAwait(false);

            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return await this.BuildApiResponseAsync<TenantResource>(response, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<ApiResponse> DeleteChildTenantAsync(string parentTenantId, string childTenantId, CancellationToken cancellationToken = default) =>
        this.DeleteChildTenantByLinkAsync($"/{parentTenantId}/marain/tenant/children/{childTenantId}", cancellationToken);

    /// <inheritdoc/>
    public async Task<ApiResponse> DeleteChildTenantByLinkAsync(string deleteTenantLink, CancellationToken cancellationToken = default)
    {
        using Activity? activity = ActivitySource.StartActivity("client.delete-child-tenant");
        activity?.SetTag("http.method", "DELETE");
        activity?.SetTag("http.url", deleteTenantLink);

        try
        {
            Uri uri = this.ConstructUri(deleteTenantLink);
            HttpRequestMessage request = this.BuildRequest(HttpMethod.Delete, uri);
            HttpResponseMessage response = await this.SendRequestAndThrowOnFailureAsync(request, cancellationToken).ConfigureAwait(false);

            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return new ApiResponse(response.StatusCode, MapHttpResponseHeadersToDictionary(response.Headers));
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
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
    public async Task<ApiResponse<TenantResource>> GetTenantByLinkAsync(string tenantLink, string? etag = null, CancellationToken cancellationToken = default)
    {
        using Activity? activity = ActivitySource.StartActivity("client.get-tenant");
        activity?.SetTag("http.method", "GET");
        activity?.SetTag("http.url", tenantLink);
        activity?.SetTag("request.has_etag", !string.IsNullOrEmpty(etag));

        try
        {
            ApiResponse<TenantResource> response = await this.GetPathAsync<TenantResource>(
                tenantLink,
                request =>
                {
                    if (!string.IsNullOrEmpty(etag))
                    {
                        request.Headers.Add("If-None-Match", etag);
                    }
                },
                cancellationToken).ConfigureAwait(false);

            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ApiResponse<TenantResource>> UpdateTenantAsync(string tenantId, string? newName, IEnumerable<KeyValuePair<string, object>>? propertiesToAddOrUpdate, IEnumerable<string>? propertiesToRemove, CancellationToken cancellationToken = default)
    {
        using Activity? activity = ActivitySource.StartActivity("client.update-tenant");
        activity?.SetTag("http.method", "PATCH");
        activity?.SetTag("http.url", $"/{tenantId}/marain/tenant");
        activity?.SetTag("tenant.id", tenantId);
        activity?.SetTag("update.has_name", !string.IsNullOrWhiteSpace(newName));

        try
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

            activity?.SetTag("patch.entry_count", updateTenantPatchEntries.Count);
            activity?.SetTag("patch.properties_to_add", propertiesToAddOrUpdate?.Count() ?? 0);
            activity?.SetTag("patch.properties_to_remove", propertiesToRemove?.Count() ?? 0);

            Uri uri = this.ConstructUri($"/{tenantId}/marain/tenant");
            HttpRequestMessage request = this.BuildRequest(HttpMethod.Patch, uri, updateTenantPatchEntries);
            HttpResponseMessage response = await this.SendRequestAndThrowOnFailureAsync(request, cancellationToken).ConfigureAwait(false);

            activity?.SetTag("http.status_code", (int)response.StatusCode);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return await this.BuildApiResponseAsync<TenantResource>(response, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}