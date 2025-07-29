// <copyright file="GetChildrenParameters.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents parameters for getting child tenants.
/// </summary>
public sealed record GetChildrenParameters
{
    /// <summary>
    /// Gets the parent tenant identifier.
    /// </summary>
    [FromRoute]
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the continuation token for paginated requests.
    /// </summary>
    [FromQuery]
    public string? ContinuationToken { get; init; }

    /// <summary>
    /// Gets the maximum number of items to return.
    /// </summary>
    [FromQuery]
    public int? MaxItems { get; init; }
}