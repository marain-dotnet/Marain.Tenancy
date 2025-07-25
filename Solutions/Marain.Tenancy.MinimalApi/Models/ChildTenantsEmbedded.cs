// <copyright file="ChildTenantsEmbedded.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the embedded tenants data in a child tenants collection response.
/// </summary>
public sealed record ChildTenantsEmbedded
{
    /// <summary>
    /// Gets the list of child tenants.
    /// </summary>
    [JsonPropertyName("tenants")]
    public IReadOnlyList<TenantResponse>? Tenants { get; init; }
}