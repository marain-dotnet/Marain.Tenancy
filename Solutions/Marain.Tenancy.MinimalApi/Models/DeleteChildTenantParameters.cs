// <copyright file="DeleteChildTenantParameters.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

/// <summary>
/// Represents parameters for deleting a child tenant.
/// </summary>
public sealed record DeleteChildTenantParameters
{
    /// <summary>
    /// Gets the parent tenant identifier.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the child tenant identifier to delete.
    /// </summary>
    public required string ChildTenantId { get; init; }
}