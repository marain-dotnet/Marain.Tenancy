using Marain.Tenancy.Client.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Marain.Tenancy.Client;

/// <summary>
/// </summary>
public interface ITenancyService
{
    /// <summary>
    /// Update a tenant
    /// </summary>
    /// <remarks>
    /// Updates the tenant
    /// </remarks>
    /// <param name="tenantId">
    /// The tenant within which the request should operate
    /// </param>
    /// <param name="body">
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    Task UpdateTenantAsync(string tenantId, IList<UpdateTenantJsonPatchEntry> body, CancellationToken? cancellationToken = default);

    /// <summary>
    /// Gets a tenant
    /// </summary>
    /// <remarks>
    /// Gets the tenant
    /// </remarks>
    /// <param name="tenantId">
    /// The tenant within which the request should operate
    /// </param>
    /// <param name="etag">
    /// The ETag of the last known version.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    Task<Tenant> GetTenantAsync(string tenantId, string? etag = default, CancellationToken? cancellationToken = default);

    /// <summary>
    /// Get all child tenants of the current tenant
    /// </summary>
    /// <remarks>
    /// Get all child tenants of the current tenant
    /// </remarks>
    /// <param name="tenantId">
    /// The tenant within which the request should operate
    /// </param>
    /// <param name="continuationToken">
    /// A continuation token for an operation where more data is available
    /// </param>
    /// <param name="maxItems">
    /// The maximum number of items to return in the request. Fewer than
    /// this number may be returned.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    Task<GetChildrenResult> GetChildrenAsync(string tenantId, string? continuationToken = default, int? maxItems = default, CancellationToken? cancellationToken = default);

    /// <summary>
    /// Create a child tenant
    /// </summary>
    /// <remarks>
    /// Creates a child tenant of the parent tenant
    /// </remarks>
    /// <param name="tenantId">
    /// The tenant within which the request should operate
    /// </param>
    /// <param name="tenantName">
    /// The name for the new tenant
    /// </param>
    /// <param name="wellKnownChildTenantGuid">
    /// The well known Guid for the new tenant. If provided, this will be
    /// used to create the child tenant Id.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    Task CreateChildTenantAsync(string tenantId, string tenantName, Guid? wellKnownChildTenantGuid = default, CancellationToken? cancellationToken = default);

    /// <summary>
    /// Delete a child tenant by ID
    /// </summary>
    /// <remarks>
    /// Deletes a child tenant of the parent tenant by ID
    /// </remarks>
    /// <param name="tenantId">
    /// The tenant within which the request should operate
    /// </param>
    /// <param name="childTenantId">
    /// The child tenant within the current tenant.
    /// </param>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    Task DeleteChildTenantAsync(string tenantId, string childTenantId, CancellationToken? cancellationToken = default);
}
