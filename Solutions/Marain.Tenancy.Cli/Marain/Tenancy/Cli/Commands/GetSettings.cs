// <copyright file="GetSettings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System.ComponentModel;
using Spectre.Console.Cli;

/// <summary>
/// Settings for the Get command.
/// </summary>
public sealed class GetSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the tenant whose details should be retrieved.
    /// </summary>
    [Description("The Id of the tenant to retrieve details for.")]
    [CommandOption("-t|--tenant")]
    public string? TenantId { get; set; }
}