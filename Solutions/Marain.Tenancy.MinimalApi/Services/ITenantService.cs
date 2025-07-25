// <copyright file="ITenantService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Services;

using Marain.Tenancy.MinimalApi.Models;

/// <summary>
/// Interface for tenant business operations.
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets a tenant by its identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="etag">The etag for conditional requests.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tenant response or null if not found.</returns>
    Task<TenantServiceResult<TenantResponse>> GetTenantAsync(string tenantId, string? etag = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new child tenant.
    /// </summary>
    /// <param name="parentTenantId">The parent tenant identifier.</param>
    /// <param name="tenantName">The name for the new child tenant.</param>
    /// <param name="wellKnownChildTenantGuid">Optional well-known GUID for the child tenant.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created tenant response.</returns>
    Task<TenantServiceResult<TenantResponse>> CreateChildTenantAsync(
        string parentTenantId, 
        string tenantName, 
        string? wellKnownChildTenantGuid = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child tenants for the specified parent tenant.
    /// </summary>
    /// <param name="parentTenantId">The parent tenant identifier.</param>
    /// <param name="maxItems">Maximum number of items to return.</param>
    /// <param name="continuationToken">Continuation token for pagination.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The child tenants response.</returns>
    Task<TenantServiceResult<ChildTenantsResponse>> GetChildTenantsAsync(
        string parentTenantId,
        int? maxItems = null,
        string? continuationToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a tenant using JSON Patch operations.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="jsonPatchDocument">The JSON patch document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated tenant response.</returns>
    Task<TenantServiceResult<TenantResponse>> UpdateTenantAsync(
        string tenantId,
        string jsonPatchDocument,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a child tenant.
    /// </summary>
    /// <param name="parentTenantId">The parent tenant identifier.</param>
    /// <param name="childTenantId">The child tenant identifier to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the delete operation.</returns>
    Task<TenantServiceResult> DeleteChildTenantAsync(
        string parentTenantId,
        string childTenantId,
        CancellationToken cancellationToken = default);
}