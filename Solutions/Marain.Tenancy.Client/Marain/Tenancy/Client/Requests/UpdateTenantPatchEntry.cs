// <copyright file="UpdateTenantPatchEntry.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client.Requests;

using System.Text.Json.Serialization;

public record UpdateTenantPatchEntry
{
    public required string Path { get; init; }

    [JsonPropertyName("op")]
    public required string Operation { get; init; }

    public object? Value { get; init; }

    public static UpdateTenantPatchEntry CreateAddOrUpdateOperation(string propertyName, object? value)
    {
        return new()
        {
            Path = $"/properties/{propertyName}",
            Operation = "add",
            Value = value,
        };
    }

    public static UpdateTenantPatchEntry CreateUpdateNameOperation(string newName)
    {
        return new()
        {
            Path = $"/name",
            Operation = "replace",
            Value = newName,
        };
    }

    public static UpdateTenantPatchEntry CreateDeleteOperation(string propertyName)
    {
        return new()
        {
            Path = $"/properties/{propertyName}",
            Operation = "remove",
        };
    }
}
