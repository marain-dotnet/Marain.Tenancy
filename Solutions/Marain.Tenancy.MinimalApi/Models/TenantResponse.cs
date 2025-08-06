// <copyright file="TenantResponse.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a tenant in API responses.
/// </summary>
public sealed record TenantResponse
{
    /// <summary>
    /// Gets the unique identifier of the tenant.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the name of the tenant.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the content type of the tenant.
    /// </summary>
    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets the properties of the tenant.
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; init; }

    /// <summary>
    /// Gets the HAL-style links for the tenant.
    /// </summary>
    [JsonPropertyName("_links")]
    public required TenantLinksResponse Links { get; init; }
}