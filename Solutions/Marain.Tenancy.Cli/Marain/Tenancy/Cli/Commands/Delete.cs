// <copyright file="Delete.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Corvus.Tenancy;
using Marain.Tenancy.Shared.Extensions;
using Marain.Tenancy.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Deletes a tenant.
/// </summary>
public class Delete(ITenantStore tenantStore, ILogger<Delete> logger) : AsyncCommand<DeleteSettings>
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.CliActivitySource);

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, DeleteSettings settings)
    {
        using Activity? activity = ActivitySource.StartActivity("cli.delete-tenant");

        activity?.SetCommandOperationTags("delete", settings.TenantId);

        if (string.IsNullOrEmpty(settings.TenantId))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Tenant Id must be provided");
            logger.LogError("Delete tenant command failed: Tenant Id must be provided");
            AnsiConsole.MarkupLine("[red]Error: Tenant Id must be provided.[/]");
            return 1;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation("Checking if tenant {TenantId} has children before deletion", settings.TenantId);

            // Check for children before deletion
            TenantCollectionResult children = await tenantStore.GetChildrenAsync(settings.TenantId, 1).ConfigureAwait(false);

            activity?.SetTag("tenant.has_children", children.Tenants.Count > 0);

            if (children.Tenants.Count > 0)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Tenant has children");
                logger.LogWarning("Cannot delete tenant {TenantId} as it has {ChildCount} children", settings.TenantId, children.Tenants.Count);
                AnsiConsole.MarkupLine(
                    $"[red]Cannot delete tenant with Id {settings.TenantId} as it has children. Remove the child tenants first.[/]");
                return 1;
            }

            // Confirmation prompt
            bool confirmed = AnsiConsole.Confirm($"Are you sure you want to delete tenant '{settings.TenantId}'?", false);
            activity?.SetTag("user.confirmed", confirmed);

            if (!confirmed)
            {
                activity?.SetStatus(ActivityStatusCode.Ok, "Deletion cancelled by user");
                logger.LogInformation("Deletion of tenant {TenantId} cancelled by user", settings.TenantId);
                AnsiConsole.MarkupLine("[yellow]Deletion cancelled.[/]");
                return 0;
            }

            logger.LogInformation("Deleting tenant {TenantId}", settings.TenantId);

            await tenantStore.DeleteTenantAsync(settings.TenantId).ConfigureAwait(false);

            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Successfully deleted tenant {TenantId} in {Duration}ms",
                settings.TenantId,
                stopwatch.ElapsedMilliseconds);

            AnsiConsole.MarkupLine($"[green]Deleted tenant with Id {settings.TenantId}[/]");

            return 0;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);

            logger.LogError(
                ex,
                "Failed to delete tenant {TenantId} after {Duration}ms",
                settings.TenantId,
                stopwatch.ElapsedMilliseconds);

            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}