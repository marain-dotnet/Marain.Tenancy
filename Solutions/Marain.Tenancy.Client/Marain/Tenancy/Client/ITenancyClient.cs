// <copyright file="ITenancyClient.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Corvus.Json;
    using Marain.Clients;
    using Marain.Tenancy.Client.Resources;

    /// <summary>
    /// Interface for a client for the Marain.Tenancy API.
    /// </summary>
    public interface ITenancyClient
    {
        /// <summary>
        /// Gets a tenant by Id.
        /// </summary>
        /// <param name="tenantId">The Id of the tenant to retrieve.</param>
        /// <param name="etag">The etag from the last time this tenant was requested.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task representing the operation status.</returns>
        Task<ApiResponse<TenantResource>> GetTenantAsync(
            string tenantId,
            string? etag = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a tenant using a link retrieved from another tenant.
        /// </summary>
        /// <param name="tenantLink">A link to the tenant to retrieve.</param>
        /// <param name="etag">The etag from the last time this tenant was requested.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task representing the operation status.</returns>
        Task<ApiResponse<TenantResource>> GetTenantByLinkAsync(
            string tenantLink,
            string? etag = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new child tenant under the specified parent tenant.
        /// </summary>
        /// <param name="parentTenantId">The Id of the parent for the new tenant.</param>
        /// <param name="tenantName">The name of the new tenant.</param>
        /// <param name="wellKnownChildTenantGuid">If required, the well-known GUID for the new tenant. Optional.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task representing the operation status.</returns>
        Task<ApiResponse<TenantResource>> CreateChildTenantAsync(
            string parentTenantId,
            string tenantName,
            string? wellKnownChildTenantGuid = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new child tenant using a link to the parent tenant.
        /// </summary>
        /// <param name="parentTenantLink">The link to the parent tenant.</param>
        /// <param name="tenantName">The name of the new tenant.</param>
        /// <param name="wellKnownChildTenantGuid">If required, the well-known GUID for the new tenant. Optional.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task representing the operation status.</returns>
        Task<ApiResponse<TenantResource>> CreateChildTenantByLinkAsync(
            string parentTenantLink,
            string tenantName,
            string? wellKnownChildTenantGuid = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of children of a specified tenant.
        /// </summary>
        /// <param name="tenantId">The Id of the parent tenant.</param>
        /// <param name="continuationToken">A continuation token from a previous request.</param>
        /// <param name="maxItems">The maximum number of items to retrieve.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task representing the operation status.</returns>
        Task<ApiResponse<ChildTenantsResource>> GetChildrenAsync(
            string tenantId,
            string? continuationToken = null,
            int? maxItems = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of children of a specified tenant using a link.
        /// </summary>
        /// <param name="childTenantsLink">The Id of the parent tenant.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task representing the operation status.</returns>
        Task<ApiResponse<ChildTenantsResource>> GetChildrenByLinkAsync(
            string childTenantsLink,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a tenant using its parent and child Ids.
        /// </summary>
        /// <param name="parentTenantId">The Id of the parent of the tenant to delete.</param>
        /// <param name="childTenantId">The Id of the tenant to delete.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task representing the operation status.</returns>
        Task<ApiResponse> DeleteChildTenantAsync(
            string parentTenantId,
            string childTenantId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a tenant using a link.
        /// </summary>
        /// <param name="deleteTenantLink">A link to the tenant to delete.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task representing the operation status.</returns>
        Task<ApiResponse> DeleteChildTenantByLinkAsync(
            string deleteTenantLink,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a tenant.
        /// </summary>
        /// <param name="tenantId">A link to the tenant to retrieve.</param>
        /// <param name="newName">The new name for the tenant, if required.</param>
        /// <param name="propertiesToAddOrUpdate">A list of properties to add or update on the tenant.</param>
        /// <param name="propertiesToRemove">A list of properties to remove from the tenant.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>A task representing the operation status.</returns>
        Task<ApiResponse<TenantResource>> UpdateTenantAsync(
            string tenantId,
            string? newName,
            IEnumerable<KeyValuePair<string, object>>? propertiesToAddOrUpdate,
            IEnumerable<string>? propertiesToRemove,
            CancellationToken cancellationToken = default);
    }
}