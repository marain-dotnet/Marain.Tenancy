// <copyright file="UpdateTenantJsonPatchEntryOperation.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

using System.Text.Json.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1602 // Enumeration items should be documented

/// <summary>
/// Possible values for <see cref="UpdateTenantJsonPatchEntry.Operation"/>.
/// </summary>
public enum UpdateTenantJsonPatchEntryOperation
{
    [JsonPropertyName("add")]
    Add,

    [JsonPropertyName("replace")]
    Replace,

    [JsonPropertyName("remove")]
    Remove,
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1602 // Enumeration items should be documented