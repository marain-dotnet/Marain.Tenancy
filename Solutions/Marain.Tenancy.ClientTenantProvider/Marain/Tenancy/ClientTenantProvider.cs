// <copyright file="ClientTenantProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Threading.Tasks;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using Marain.Clients;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Resources;
using Marain.Tenancy.Mappers;
using Marain.Tenancy.Shared.Extensions;
using Marain.Tenancy.Shared.Telemetry;
using Microsoft.Extensions.Logging;

/// <summary>
/// An <see cref="ITenantProvider"/> built over a Marain tenancy instance.
/// </summary>
public class ClientTenantProvider(RootTenant root, ITenancyClient apiClient, ITenantMapper tenantMapper, ILogger<ClientTenantProvider>? logger = null) : ITenantProvider
{
    // Telemetry infrastructure
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.BusinessActivitySource);
    private static readonly Meter Meter = new(TelemetryConstants.TenancyMeter);
    private static readonly Counter<long> TenantOperationsCounter =
        Meter.CreateCounter<long>("tenant.business.operations.total", "operations", "Total business operations");

    private static readonly Histogram<double> TenantOperationDuration =
        Meter.CreateHistogram<double>("tenant.business.operation.duration", "ms", "Business operation duration");

    private static readonly Counter<long> TenantErrorsCounter =
        Meter.CreateCounter<long>("tenant.business.errors.total", "errors", "Total business errors");

    private readonly ILogger<ClientTenantProvider>? logger = logger;

    /// <summary>
    /// Gets the root tenant.
    /// </summary>
    public RootTenant Root { get; } = root ?? throw new ArgumentNullException(nameof(root));

    /// <summary>
    /// Gets the tenancy service.
    /// </summary>
    protected ITenancyClient TenantApiClient { get; } = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

    /// <summary>
    /// Gets the tenant mapper.
    /// </summary>
    protected ITenantMapper TenantMapper { get; } = tenantMapper ?? throw new ArgumentNullException(nameof(tenantMapper));

    /// <inheritdoc/>
    public async Task<ITenant> GetTenantAsync(string tenantId, string? eTag = null)
    {
        using Activity? activity = ActivitySource.StartActivity("business.tenant.get");
        activity?.SetTenantOperationTags(TelemetryConstants.OperationTypes.Get, tenantId);
        if (!string.IsNullOrEmpty(eTag))
        {
            activity?.SetTag("etag", eTag);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        this.logger?.LogDebug("Getting tenant {TenantId}, eTag: {ETag}", tenantId, eTag);

        try
        {
            // The root tenant is a special case - it lives just in memory. This is because
            // services use it to configure service-specific defaults.
            if (tenantId == this.Root.Id)
            {
                // Record successful operation
                TenantOperationsCounter.Add(
                    1,
                    new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
                    new KeyValuePair<string, object?>("status", "success"));
                TenantOperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
                activity?.SetStatus(ActivityStatusCode.Ok);
                this.logger?.LogDebug("Successfully retrieved root tenant");
                return this.Root;
            }

            ApiResponse<TenantResource> tenantResponse = await this.TenantApiClient.GetTenantAsync(tenantId, eTag).ConfigureAwait(false);

            tenantResponse.Headers.TryGetValue("etag", out string? etagValue);
            ITenant result = this.TenantMapper.MapTenant(tenantResponse.Body, etagValue);

            // Record successful operation
            TenantOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
                new KeyValuePair<string, object?>("status", "success"));
            TenantOperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);
            this.logger?.LogDebug("Successfully retrieved tenant {TenantId} via API client", tenantId);

            return result;
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Tenant not found");
            TenantErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
                new KeyValuePair<string, object?>("error.type", "not_found"));
            TenantOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogWarning("Tenant not found: {TenantId}", tenantId);
            throw new TenantNotFoundException();
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Invalid tenant request");
            TenantErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
                new KeyValuePair<string, object?>("error.type", "bad_request"));
            TenantOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogError(ex, "Bad request for tenant {TenantId}: {Message}", tenantId, ex.Message);
            throw new ArgumentException($"Invalid tenant request: {ex.Message}");
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotModified)
        {
            activity?.SetStatus(ActivityStatusCode.Ok, "Not modified");
            TenantOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
                new KeyValuePair<string, object?>("status", "not_modified"));

            this.logger?.LogDebug("Tenant {TenantId} not modified", tenantId);
            throw new TenantNotModifiedException();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            TenantErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));
            TenantOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogError(ex, "Failed to get tenant {TenantId}", tenantId);
            throw;
        }
    }
}