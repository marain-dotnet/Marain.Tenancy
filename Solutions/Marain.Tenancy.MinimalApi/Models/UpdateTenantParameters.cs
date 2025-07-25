// <copyright file="UpdateTenantParameters.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

/// <summary>
/// Represents parameters for updating a tenant.
/// </summary>
public sealed record UpdateTenantParameters
{
    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    required public string TenantId { get; init; }
}