// <copyright file="Create.cs" company="Endjin Limited">
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
/// Creates a new tenant.
/// </summary>
public class Create(ITenantStore tenantStore, ILogger<Create> logger) : AsyncCommand<CreateSettings>
{
    /// <summary>
    /// Gets the <see cref="ActivitySource" /> for the command.
    /// </summary>
    public static ActivitySource ActivitySource { get; } = new(TelemetryConstants.CliActivitySource);

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, CreateSettings settings)
    {
        using Activity? activity = ActivitySource.StartActivity("cli.create-tenant");

        string tenantId = string.IsNullOrEmpty(settings.TenantId)
            ? tenantStore.Root.Id
            : settings.TenantId;

        activity?.SetCommandOperationTags("create", tenantId);
        activity?.SetTag("tenant.name", settings.Name);

        if (string.IsNullOrEmpty(settings.Name))
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Name must be supplied");
            logger.LogError("Create tenant command failed: Name must be supplied");
            AnsiConsole.MarkupLine("[red]Error: Name must be supplied[/]");
            return 1;
        }

        Guid wellKnownGuid = string.IsNullOrEmpty(settings.WellKnownTenantGuid)
            ? Guid.NewGuid()
            : Guid.Parse(settings.WellKnownTenantGuid);

        activity?.SetTag("tenant.well_known_guid", wellKnownGuid.ToString());
        activity?.SetTag("tenant.is_well_known", !string.IsNullOrEmpty(settings.WellKnownTenantGuid));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation(
                "Creating child tenant with name {TenantName} under parent {ParentTenantId} (WellKnownGuid: {WellKnownGuid})",
                settings.Name,
                tenantId,
                wellKnownGuid);

            ITenant child = await tenantStore.CreateWellKnownChildTenantAsync(
                tenantId,
                wellKnownGuid,
                settings.Name).ConfigureAwait(false);

            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("tenant.created_id", child.Id);
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Successfully created child tenant {TenantId} with name {TenantName} under parent {ParentTenantId} in {Duration}ms",
                child.Id,
                settings.Name,
                tenantId,
                stopwatch.ElapsedMilliseconds);

            AnsiConsole.MarkupLine($"[green]Created new child tenant with Id {child.Id} and name {child.Name}[/]");

            return 0;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);

            logger.LogError(
                ex,
                "Failed to create child tenant {TenantName} under parent {ParentTenantId} after {Duration}ms",
                settings.Name,
                tenantId,
                stopwatch.ElapsedMilliseconds);

            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}