// <copyright file="Get.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli.Commands;

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Corvus.Json.Serialization;
using Corvus.Tenancy;
using Marain.Tenancy.Shared.Extensions;
using Marain.Tenancy.Shared.Telemetry;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Retrieves all details for the specified tenant.
/// </summary>
public class Get(ITenantProvider tenantProvider, IJsonSerializerOptionsProvider serializationSettingsProvider, ILogger<Get> logger) : AsyncCommand<GetSettings>
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.CliActivitySource);

    /// <summary>
    /// Executes the command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public override async Task<int> ExecuteAsync(CommandContext context, GetSettings settings)
    {
        using Activity? activity = ActivitySource.StartActivity("cli.get-tenant");

        string tenantId = string.IsNullOrEmpty(settings.TenantId)
            ? tenantProvider.Root.Id
            : settings.TenantId;

        activity?.SetCommandOperationTags("get", tenantId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation("Getting tenant {TenantId}", tenantId);

            ITenant tenant = await tenantProvider.GetTenantAsync(tenantId).ConfigureAwait(false);

            string result = JsonSerializer.Serialize(
                tenant,
                serializationSettingsProvider.Instance);

            AnsiConsole.WriteLine(result);

            stopwatch.Stop();
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("tenant.found", true);
            activity?.SetTag("operation.duration_ms", stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Successfully retrieved tenant {TenantId} in {Duration}ms",
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
                "Failed to get tenant {TenantId} after {Duration}ms",
                tenantId,
                stopwatch.ElapsedMilliseconds);

            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}