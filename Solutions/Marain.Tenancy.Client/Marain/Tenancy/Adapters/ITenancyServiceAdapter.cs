// <copyright file="ITenancyServiceAdapter.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Adapters;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Models;
using Microsoft.Rest;

/// <summary>
/// Adapter interface for migrating from AutoRest client to Kiota client.
/// This provides backward compatibility for existing code using the AutoRest client.
/// </summary>
public interface ITenancyServiceAdapter
{
    /// <summary>
    /// Gets a tenant by ID with optional etag handling.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="ifNoneMatch">Optional ETag for conditional requests.</param>
    /// <param name="customHeaders">Optional custom headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP operation response with tenant data.</returns>
    Task<HttpOperationResponse<Tenant>> GetTenantWithHttpMessagesAsync(string tenantId, string? ifNoneMatch = null, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a tenant with JSON patch operations.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="patchDocument">The JSON patch document.</param>
    /// <param name="customHeaders">Optional custom headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP operation response with updated tenant data.</returns>
    Task<HttpOperationResponse<Tenant>> UpdateTenantWithHttpMessagesAsync(string tenantId, IList<UpdateTenantJsonPatchEntry> patchDocument, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child tenants with optional paging.
    /// </summary>
    /// <param name="tenantId">The parent tenant ID.</param>
    /// <param name="continuationToken">Optional continuation token.</param>
    /// <param name="maxItems">Optional maximum items to return.</param>
    /// <param name="customHeaders">Optional custom headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP operation response with child tenants data.</returns>
    Task<HttpOperationResponse<ChildTenants>> GetChildrenWithHttpMessagesAsync(string tenantId, string? continuationToken = null, int? maxItems = null, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a child tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant ID.</param>
    /// <param name="tenantName">The name for the new tenant.</param>
    /// <param name="wellKnownChildTenantGuid">Optional well-known GUID for the child tenant.</param>
    /// <param name="customHeaders">Optional custom headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP operation response.</returns>
    Task<HttpOperationResponse> CreateChildTenantWithHttpMessagesAsync(string tenantId, string tenantName, System.Guid? wellKnownChildTenantGuid = null, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a child tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant ID.</param>
    /// <param name="childTenantId">The child tenant ID to delete.</param>
    /// <param name="customHeaders">Optional custom headers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>HTTP operation response.</returns>
    Task<HttpOperationResponse> DeleteChildTenantWithHttpMessagesAsync(string tenantId, string childTenantId, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default);
}