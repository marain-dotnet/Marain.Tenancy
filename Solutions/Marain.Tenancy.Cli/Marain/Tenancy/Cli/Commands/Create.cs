// <copyright file="Create.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System;
using System.Threading.Tasks;
using Corvus.Tenancy;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Creates a new tenant.
/// </summary>
public class Create : AsyncCommand<CreateSettings>
{
    private readonly ITenantStore tenantStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="Create"/> class.
    /// </summary>
    /// <param name="tenantStore">The tenant store that will be used to create the new tenant.</param>
    public Create(ITenantStore tenantStore)
    {
        this.tenantStore = tenantStore;
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, CreateSettings settings)
    {
        string tenantId = string.IsNullOrEmpty(settings.TenantId)
            ? this.tenantStore.Root.Id
            : settings.TenantId;

        if (string.IsNullOrEmpty(settings.Name))
        {
            AnsiConsole.MarkupLine("[red]Error: Name must be supplied[/]");
            return 1;
        }

        Guid wellKnownGuid = string.IsNullOrEmpty(settings.WellKnownTenantGuid)
            ? Guid.NewGuid()
            : Guid.Parse(settings.WellKnownTenantGuid);

        ITenant child = await this.tenantStore.CreateWellKnownChildTenantAsync(
            tenantId,
            wellKnownGuid,
            settings.Name).ConfigureAwait(false);

        AnsiConsole.MarkupLine($"[green]Created new child tenant with Id {child.Id} and name {child.Name}[/]");

        return 0;
    }
}