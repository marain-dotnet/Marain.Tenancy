// <copyright file="Delete.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System.Threading.Tasks;
using Corvus.Tenancy;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Deletes a tenant.
/// </summary>
public class Delete(ITenantStore tenantStore) : AsyncCommand<DeleteSettings>
{
    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteSettings settings)
    {
        if (string.IsNullOrEmpty(settings.TenantId))
        {
            AnsiConsole.MarkupLine("[red]Error: Tenant Id must be provided.[/]");
            return 1;
        }

        // Check for children before deletion
        TenantCollectionResult children = await tenantStore.GetChildrenAsync(settings.TenantId, 1).ConfigureAwait(false);

        if (children.Tenants.Count > 0)
        {
            AnsiConsole.MarkupLine(
                $"[red]Cannot delete tenant with Id {settings.TenantId} as it has children. Remove the child tenants first.[/]");
            return 1;
        }

        // Confirmation prompt
        bool confirmed = AnsiConsole.Confirm($"Are you sure you want to delete tenant '{settings.TenantId}'?", false);

        if (!confirmed)
        {
            AnsiConsole.MarkupLine("[yellow]Deletion cancelled.[/]");
            return 0;
        }

        await tenantStore.DeleteTenantAsync(settings.TenantId).ConfigureAwait(false);

        AnsiConsole.MarkupLine($"[green]Deleted tenant with Id {settings.TenantId}[/]");

        return 0;
    }
}