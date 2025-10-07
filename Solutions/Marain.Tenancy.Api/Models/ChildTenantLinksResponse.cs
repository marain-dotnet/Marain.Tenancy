// <copyright file="ChildTenantLinksResponse.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Links collection for the child tenants API response.
/// </summary>
public sealed record ChildTenantLinksResponse
{
    /// <summary>
    /// Gets the link to the endpoint that generated this result.
    /// </summary>
    [JsonPropertyName("self")]
    public required LinkResponse Self { get; init; }

    /// <summary>
    /// Gets the link to the next page of results, if they exist.
    /// </summary>
    [JsonPropertyName("next")]
    public required LinkResponse? Next { get; init; }

    /// <summary>
    /// Gets the list of get tenant links.
    /// </summary>
    [JsonPropertyName("getTenant")]
    public required IReadOnlyList<LinkResponse> GetTenant { get; init; }

    /// <summary>
    /// Gets the list of delete tenant links.
    /// </summary>
    [JsonPropertyName("deleteTenant")]
    public required IReadOnlyList<LinkResponse> DeleteTenant { get; init; }
}