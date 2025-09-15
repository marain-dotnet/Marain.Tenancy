// <copyright file="List.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Corvus.Json.Serialization;
using Corvus.Tenancy;
using Marain.Tenancy.Shared.Extensions;
using Marain.Tenancy.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Lists children of the specified tenant.
/// </summary>
public class List(ITenantStore tenantStore, IJsonSerializerOptionsProvider serializationSettingsProvider, ILogger<List> logger) : AsyncCommand<ListSettings>
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.CliActivitySource);

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, ListSettings settings)
    {
        using Activity? activity = ActivitySource.StartActivity("cli.list-tenants");

        string tenantId = string.IsNullOrEmpty(settings.TenantId)
            ? tenantStore.Root.Id
            : settings.TenantId;

        activity?.SetCommandOperationTags("list", tenantId);
        activity?.SetTag("list.include_name", settings.Name);
        activity?.SetTag("list.include_properties", settings.IncludeProperties?.Length > 0);
        activity?.SetTag("list.properties_count", settings.IncludeProperties?.Length ?? 0);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation("Listing child tenants for {TenantId}", tenantId);

            string? continuationToken = null;
            List<string> childTenantIds = [];
            int totalPages = 0;

            do
            {
                totalPages++;
                TenantCollectionResult children = await tenantStore.GetChildrenAsync(
                    tenantId,
                    20,
                    continuationToken).ConfigureAwait(false);

                childTenantIds.AddRange(children.Tenants);
                continuationToken = children.ContinuationToken;
            }
            while (!string.IsNullOrEmpty(continuationToken));

            activity?.SetTag("result.total_children", childTenantIds.Count);
            activity?.SetTag("result.pages_fetched", totalPages);

            logger.LogInformation(
                "Found {ChildCount} child tenants for {TenantId} across {PageCount} pages",
                childTenantIds.Count,
                tenantId,
                totalPages);

            if (settings.Name || settings.IncludeProperties?.Length > 0)
            {
                await this.LoadAndOutputTenantDetailsAsync(childTenantIds, settings).ConfigureAwait(false);
            }
            else
            {
                OutputTenantIds(childTenantIds);
            }

            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Successfully listed {ChildCount} child tenants for {TenantId} in {Duration}ms",
                childTenantIds.Count,
                tenantId,
                stopwatch.ElapsedMilliseconds);

            return 0;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);

            logger.LogError(
                ex,
                "Failed to list child tenants for {TenantId} after {Duration}ms",
                tenantId,
                stopwatch.ElapsedMilliseconds);

            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static void OutputTenantIds(List<string> children)
    {
        AnsiConsole.MarkupLine("[bold]Child Tenant Ids:[/]");

        foreach (string current in children)
        {
            AnsiConsole.WriteLine($"  {current}");
        }
    }

    private async Task LoadAndOutputTenantDetailsAsync(List<string> children, ListSettings settings)
    {
        IEnumerable<Task<ITenant>> detailsTasks = children.Select(x => tenantStore.GetTenantAsync(x));

        ITenant[] tenants = await Task.WhenAll(detailsTasks).ConfigureAwait(false);

        List<string> headings = ["Id"];

        if (settings.Name)
        {
            headings.Add("Name");
        }

        if (settings.IncludeProperties != null)
        {
            headings.AddRange(settings.IncludeProperties);
        }

        var table = new Table();

        foreach (string heading in headings)
        {
            table.AddColumn(heading);
        }

        foreach (ITenant tenant in tenants)
        {
            List<string> result = [tenant.Id];

            if (settings.Name)
            {
                result.Add(tenant.Name);
            }

            if (settings.IncludeProperties != null)
            {
                foreach (string prop in settings.IncludeProperties)
                {
                    tenant.Properties.TryGet(prop, out JsonNode propValue);
                    result.Add(propValue.ToJsonString(serializationSettingsProvider.Instance) ?? "{not set}");
                }
            }

            table.AddRow([.. result]);
        }

        AnsiConsole.Write(table);
    }
}