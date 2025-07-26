// <copyright file="UpdateTenantRequest.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

/// <summary>
/// Represents a request to update a tenant.
/// </summary>
public sealed record UpdateTenantRequest
{
    /// <summary>
    /// Gets the tenant name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the tenant description.
    /// </summary>
    public string? Description { get; init; }
}