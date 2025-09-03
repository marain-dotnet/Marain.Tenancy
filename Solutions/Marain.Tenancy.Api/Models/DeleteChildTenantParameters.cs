// <copyright file="DeleteChildTenantParameters.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Models;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents parameters for deleting a child tenant.
/// </summary>
public sealed record DeleteChildTenantParameters
{
    /// <summary>
    /// Gets the parent tenant identifier.
    /// </summary>
    [FromRoute]
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the child tenant identifier to delete.
    /// </summary>
    [FromRoute]
    public required string ChildTenantId { get; init; }
}