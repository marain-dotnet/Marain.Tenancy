// <copyright file="CreateChildTenantParameters.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents parameters for creating a child tenant.
/// </summary>
public sealed record CreateChildTenantParameters
{
    /// <summary>
    /// Gets the parent tenant identifier.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the name for the new child tenant.
    /// </summary>
    [FromQuery]
    public required string TenantName { get; init; }

    /// <summary>
    /// Gets the well-known GUID for the new child tenant.
    /// </summary>
    [FromQuery]
    public string? WellKnownChildTenantGuid { get; init; }
}