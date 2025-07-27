// <copyright file="TenancyService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client;

using System;
using System.Threading;
using System.Threading.Tasks;
using Marain.Tenancy.Client.Models;

/// <summary>
/// Implementation of the tenancy service.
/// This wraps the generated Kiota client with a simplified interface.
/// </summary>
/// <param name="client">The generated Kiota client.</param>
public class TenancyService(TenancyApiClient client) : ITenancyService
{
    private readonly TenancyApiClient client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc/>
    public async Task<TenantResponse?> GetTenantAsync(string tenantId, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentNullException(nameof(tenantId));
        }

        return await client[tenantId].Marain.Tenant.GetAsync(requestConfiguration =>
        {
            if (!string.IsNullOrWhiteSpace(ifNoneMatch))
            {
                requestConfiguration.Headers.Add("If-None-Match", ifNoneMatch);
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<TenantResponse?> UpdateTenantAsync(string tenantId, UpdateTenantRequestJsonPatchDocument patchOperations, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentNullException(nameof(tenantId));
        }

        if (patchOperations == null)
        {
            throw new ArgumentNullException(nameof(patchOperations));
        }

        return await client[tenantId].Marain.Tenant.PatchAsync(patchOperations, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ChildTenantsResponse?> GetChildTenantsAsync(string tenantId, string? continuationToken = null, int? maxItems = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentNullException(nameof(tenantId));
        }

        return await client[tenantId].Marain.Tenant.Children.GetAsync(requestConfiguration =>
        {
            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                requestConfiguration.QueryParameters.ContinuationToken = continuationToken;
            }

            if (maxItems.HasValue)
            {
                requestConfiguration.QueryParameters.MaxItems = maxItems.Value;
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public Task CreateChildTenantAsync(string tenantId, CreateChildTenantRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentNullException(nameof(tenantId));
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // TODO: The Kiota-generated client doesn't seem to have a PostAsync method for creating child tenants
        // This might be because the OpenAPI spec needs to be updated or there's an issue with the generation
        // For now, throw NotImplementedException until we can resolve the Kiota generation issue
        throw new NotImplementedException("CreateChildTenantAsync is not yet implemented in the Kiota client. The generated client is missing the POST method for child tenant creation.");
    }

    /// <inheritdoc/>
    public async Task DeleteChildTenantAsync(string tenantId, string childTenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentNullException(nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(childTenantId))
        {
            throw new ArgumentNullException(nameof(childTenantId));
        }

        await client[tenantId].Marain.Tenant.Children[childTenantId].DeleteAsync(cancellationToken: cancellationToken);
    }
}