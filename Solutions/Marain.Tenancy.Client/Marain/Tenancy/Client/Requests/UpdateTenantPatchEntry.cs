// <copyright file="UpdateTenantPatchEntry.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client.Requests;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a JSON Patch entry for updating tenant properties.
/// </summary>
public record UpdateTenantPatchEntry
{
    /// <summary>
    /// Gets the path to the property being updated.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the operation type (add, replace, remove).
    /// </summary>
    [JsonPropertyName("op")]
    public required string Operation { get; init; }

    /// <summary>
    /// Gets the value to set (null for remove operations).
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Creates a patch entry for adding or updating a tenant property.
    /// </summary>
    /// <param name="propertyName">The name of the property to add or update.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>A patch entry for the add operation.</returns>
    public static UpdateTenantPatchEntry CreateAddOrUpdateOperation(string propertyName, object? value)
    {
        return new()
        {
            Path = $"/properties/{propertyName}",
            Operation = "add",
            Value = value,
        };
    }

    /// <summary>
    /// Creates a patch entry for updating the tenant name.
    /// </summary>
    /// <param name="newName">The new name for the tenant.</param>
    /// <returns>A patch entry for the name update operation.</returns>
    public static UpdateTenantPatchEntry CreateUpdateNameOperation(string newName)
    {
        return new()
        {
            Path = $"/name",
            Operation = "replace",
            Value = newName,
        };
    }

    /// <summary>
    /// Creates a patch entry for removing a tenant property.
    /// </summary>
    /// <param name="propertyName">The name of the property to remove.</param>
    /// <returns>A patch entry for the remove operation.</returns>
    public static UpdateTenantPatchEntry CreateDeleteOperation(string propertyName)
    {
        return new()
        {
            Path = $"/properties/{propertyName}",
            Operation = "remove",
        };
    }
}