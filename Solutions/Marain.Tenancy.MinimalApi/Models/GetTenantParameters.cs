// <copyright file="GetTenantParameters.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents parameters for getting a tenant.
/// </summary>
public sealed record GetTenantParameters
{
    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    [FromRoute]
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the If-None-Match header value for conditional requests.
    /// </summary>
    [FromHeader(Name = "If-None-Match")]
    public string? IfNoneMatch { get; init; }
}