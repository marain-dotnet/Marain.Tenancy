// <copyright file="UpdateTenantJsonPatchEntry.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a request to update a property of a tenant.
/// </summary>
public sealed record UpdateTenantJsonPatchEntry
{
    /// <summary>
    /// Gets the update property path. A JSON-Pointer. Either /name or /properties/propertyName.
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>
    /// Gets the operation to be performed.
    /// </summary>
    [JsonPropertyName("op")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required UpdateTenantJsonPatchEntryOperation Operation { get; init; }

    /// <summary>
    /// Gets the value to add or set.
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; init; }
}