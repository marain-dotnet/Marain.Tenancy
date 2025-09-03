// <copyright file="DeleteSettings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System.ComponentModel;
using Spectre.Console.Cli;

/// <summary>
/// Settings for the Delete command.
/// </summary>
public sealed class DeleteSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the Id of the tenant to be deleted.
    /// </summary>
    [Description("The Id of the parent tenant.")]
    [CommandOption("-t|--tenant")]
    public string? TenantId { get; set; }
}