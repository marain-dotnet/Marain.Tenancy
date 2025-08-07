// <copyright file="UpdateTenantParameters.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents parameters for updating a tenant.
/// </summary>
public sealed record UpdateTenantParameters
{
    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    [FromRoute]
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the JSON patch documents for updating the tenant.
    /// </summary>
    [FromBody]
    public required UpdateTenantJsonPatchEntry[] UpdateTenantJsonPatchArray { get; init; }
}