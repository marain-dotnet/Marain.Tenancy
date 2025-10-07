// <copyright file="CreateSettings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System.ComponentModel;
using Spectre.Console.Cli;

/// <summary>
/// Settings for the Create command.
/// </summary>
public sealed class CreateSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the Id of the tenant that should be the parent of the new tenant.
    /// </summary>
    [Description("The Id of the parent tenant. Omit if the child should be a parent of the root tenant.")]
    [CommandOption("-t|--tenant")]
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the name of the new tenant.
    /// </summary>
    [Description("The name of the new tenant.")]
    [CommandOption("-n|--name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the well-known GUID of the new tenant.
    /// </summary>
    [Description("The well-known GUID of the new tenant.")]
    [CommandOption("-g|--guid")]
    public string? WellKnownTenantGuid { get; set; }
}