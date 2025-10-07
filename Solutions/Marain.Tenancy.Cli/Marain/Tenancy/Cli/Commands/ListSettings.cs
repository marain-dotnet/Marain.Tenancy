// <copyright file="ListSettings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System.ComponentModel;
using Spectre.Console.Cli;

/// <summary>
/// Settings for the List command.
/// </summary>
public sealed class ListSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the tenant whose children should be retrieved.
    /// </summary>
    [Description("The Id of the tenant to retrieve children for. Leave blank to retrieve children of the root tenant.")]
    [CommandOption("-t|--tenant")]
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tenant name should be output.
    /// </summary>
    [Description("Indicates that the tenant name should be displayed")]
    [CommandOption("-n|--name")]
    public bool Name { get; set; }

    /// <summary>
    /// Gets or sets a value containing the names of specific properties that should be included in the output.
    /// </summary>
    [Description("The names of tenant properties to include in the output. If omitted, only the tenant Ids will be listed.")]
    [CommandOption("-p|--property")]
    public string[]? IncludeProperties { get; set; }
}