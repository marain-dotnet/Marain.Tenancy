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
    required public string TenantId { get; init; }
    
    /// <summary>
    /// Gets the child tenant identifier to delete.
    /// </summary>
    required public string ChildTenantId { get; init; }
}