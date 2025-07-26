// <copyright file="TenantParameters.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

/// <summary>
/// Represents parameters for tenant-related operations.
/// </summary>
public sealed record TenantParameters
{
    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the tenant name for creation operations.
    /// </summary>
    public string? TenantName { get; init; }

    /// <summary>
    /// Gets the well-known GUID for child tenant creation.
    /// </summary>
    public string? WellKnownChildTenantGuid { get; init; }

    /// <summary>
    /// Gets the child tenant identifier for operations on child tenants.
    /// </summary>
    public string? ChildTenantId { get; init; }

    /// <summary>
    /// Gets the continuation token for paginated requests.
    /// </summary>
    public string? ContinuationToken { get; init; }

    /// <summary>
    /// Gets the maximum number of items to return.
    /// </summary>
    public int? MaxItems { get; init; }

    /// <summary>
    /// Gets the If-None-Match header value for conditional requests.
    /// </summary>
    public string? IfNoneMatch { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a create child tenant request.
    /// </summary>
    public bool IsCreateChildTenantRequest { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a delete child tenant request.
    /// </summary>
    public bool IsDeleteChildTenantRequest { get; init; }
}