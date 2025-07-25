// <copyright file="TenantResponse.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using System.Collections.Frozen;
using System.Text.Json.Serialization;
using Corvus.Tenancy;

/// <summary>
/// Represents a tenant in API responses.
/// </summary>
public sealed record TenantResponse
{
    /// <summary>
    /// Gets the unique identifier of the tenant.
    /// </summary>
    [JsonPropertyName("id")]
    required public string Id { get; init; }
    
    /// <summary>
    /// Gets the name of the tenant.
    /// </summary>
    [JsonPropertyName("name")]
    required public string Name { get; init; }
    
    /// <summary>
    /// Gets the content type of the tenant.
    /// </summary>
    [JsonPropertyName("contentType")]
    required public string ContentType { get; init; }
    
    /// <summary>
    /// Gets the properties of the tenant.
    /// </summary>
    [JsonPropertyName("properties")]
    public IPropertyBag? Properties { get; init; }
    
    /// <summary>
    /// Gets the HAL-style links for the tenant.
    /// </summary>
    [JsonPropertyName("_links")]
    public FrozenDictionary<string, LinkResponse>? Links { get; init; }
}