// <copyright file="CreateChildTenantRequest.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client.Requests;

/// <summary>
/// Request body for creating a child tenant.
/// </summary>
public record CreateChildTenantRequest
{
    /// <summary>
    /// Gets the name of the new tenant.
    /// </summary>
    public required string TenantName { get; init; }

    /// <summary>
    /// Gets a well known GUID to use when generating the tenant Id.
    /// </summary>
    public string? WellKnownChildTenantGuid { get; init; }
}