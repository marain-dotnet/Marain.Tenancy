// <copyright file="ChildTenantsResponse.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using System.Collections.Frozen;
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
    public FrozenDictionary<string, LinkResponse>? Links { get; init; }

    /// <summary>
    /// Gets the embedded child tenants data.
    /// </summary>
    [JsonPropertyName("_embedded")]
    public ChildTenantsEmbedded? Embedded { get; init; }

    /// <summary>
    /// Gets the continuation token for pagination.
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; init; }
}