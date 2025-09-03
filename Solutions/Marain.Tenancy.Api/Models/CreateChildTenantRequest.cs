// <copyright file="CreateChildTenantRequest.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Models;

/// <summary>
/// Represents a request to create a child tenant.
/// </summary>
public sealed record CreateChildTenantRequest
{
    /// <summary>
    /// Gets the name for the new child tenant.
    /// </summary>
    public required string TenantName { get; init; }

    /// <summary>
    /// Gets the well-known GUID for the new child tenant.
    /// </summary>
    public string? WellKnownChildTenantGuid { get; init; }
}