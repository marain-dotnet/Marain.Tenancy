// <copyright file="LinkResponse.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a HAL-style link in API responses.
/// </summary>
public sealed record LinkResponse
{
    /// <summary>
    /// Gets the URI of the target resource.
    /// </summary>
    [JsonPropertyName("href")]
    required public string Href { get; init; }
    
    /// <summary>
    /// Gets a value indicating whether the href property is a URI Template.
    /// </summary>
    [JsonPropertyName("templated")]
    public bool Templated { get; init; } = false;
    
    /// <summary>
    /// Gets the media type indication of the target resource.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }
    
    /// <summary>
    /// Gets the secondary key for selecting link objects.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    
    /// <summary>
    /// Gets the human-readable identifier for the link.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }
}