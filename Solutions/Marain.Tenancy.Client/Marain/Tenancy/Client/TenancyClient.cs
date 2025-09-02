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
using Corvus.Json.Serialization;
using Marain.Clients;
using Marain.Tenancy.Client.Resources;

public class TenancyClient(HttpClient client, JsonSerializerOptions serializerOptions) : ClientBase(client, serializerOptions), ITenancyClient
{
    public Task<ApiResponse<TenantResource>> CreateChildTenantAsync(string parentTenantId, string tenantName, string? wellKnownChildTenantGuid = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<ApiResponse<TenantResource>> CreateChildTenantByLinkAsync(string parentTenantLink, string? wellKnownChildTenantGuid = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<ApiResponse> DeleteChildTenantAsync(string parentTenantId, string childTenantId, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<ApiResponse> DeleteChildTenantByLinkAsync(string deleteTenantLink, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<ApiResponse<ChildTenantsResource>> GetChildrenAsync(string tenantId, string? continuationToken = null, int? maxItems = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<ApiResponse<ChildTenantsResource>> GetChildrenByLinkAsync(string childTenantsLink, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<ApiResponse<TenantResource>> GetTenantAsync(string tenantId, string? etag = null, CancellationToken cancellationToken = default) =>
        this.GetPathAsync<TenantResource>(
            $"/{tenantId}/marain/tenant",
            request => request.Headers.Add("If-Not-Modified", etag),
            cancellationToken);

    public Task<ApiResponse<TenantResource>> GetTenantByLinkAsync(string tenantLink, string? etag = null, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }

    public Task<ApiResponse<TenantResource>> UpdateTenantAsync(string tenantId, string? newName, IEnumerable<KeyValuePair<string, object>>? propertiesToAddOrUpdate, IEnumerable<string>? propertiesToRemove, CancellationToken cancellationToken = default)
    {
        throw new System.NotImplementedException();
    }
}
