// <copyright file="ChildTenantsResponse.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a collection of child tenants in API responses.
/// </summary>
public sealed record ChildTenantsResponse
{
    /// <summary>
    /// Gets the HAL-style links for the child tenants collection.
    /// </summary>
    [JsonPropertyName("_links")]
    public required ChildTenantLinksResponse Links { get; init; }

    /// <summary>
    /// Gets the maximum items that were requested.
    /// </summary>
    [JsonPropertyName("maxItems")]
    public required int MaxItems { get; init; }

    /// <summary>
    /// Gets the continuation token for pagination.
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; init; }
}