// <copyright file="ITenancyService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client;

using System.Threading;
using System.Threading.Tasks;
using Marain.Tenancy.Client.Models;

/// <summary>
/// Interface defining the tenancy service operations.
/// This provides a simplified interface over the generated Kiota client.
/// </summary>
public interface ITenancyService
{
    /// <summary>
    /// Gets a tenant by ID.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="ifNoneMatch">Optional ETag for conditional requests.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tenant response.</returns>
    Task<TenantResponse?> GetTenantAsync(string tenantId, string? ifNoneMatch = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a tenant using JSON Patch operations.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="patchOperations">The JSON Patch operations.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated tenant response.</returns>
    Task<TenantResponse?> UpdateTenantAsync(string tenantId, UpdateTenantRequestJsonPatchDocument patchOperations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child tenants of a tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant ID.</param>
    /// <param name="continuationToken">Optional continuation token for paging.</param>
    /// <param name="maxItems">Optional maximum number of items to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The child tenants response.</returns>
    Task<ChildTenantsResponse?> GetChildTenantsAsync(string tenantId, string? continuationToken = null, int? maxItems = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a child tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant ID.</param>
    /// <param name="request">The create child tenant request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateChildTenantAsync(string tenantId, CreateChildTenantRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a child tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant ID.</param>
    /// <param name="childTenantId">The child tenant ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteChildTenantAsync(string tenantId, string childTenantId, CancellationToken cancellationToken = default);
}