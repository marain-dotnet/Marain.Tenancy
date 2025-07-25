// <copyright file="ErrorResponse.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.ErrorHandling;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an error response following RFC 7807 Problem Details format.
/// </summary>
public sealed record ErrorResponse
{
    /// <summary>
    /// Gets the error type URI.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Gets the error title.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    [JsonPropertyName("status")]
    public required int Status { get; init; }

    /// <summary>
    /// Gets the detailed error description.
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    /// <summary>
    /// Gets the instance URI that identifies the specific occurrence of the problem.
    /// </summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; init; }

    /// <summary>
    /// Gets additional properties for the error.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? Extensions { get; init; }
}