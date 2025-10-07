// <copyright file="ApiTelemetryService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Telemetry;

using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Marain.Tenancy.Shared.Extensions;
using Marain.Tenancy.Shared.Telemetry;

/// <summary>
/// Provides telemetry services for the API layer including activity sources and meters.
/// </summary>
public class ApiTelemetryService : IDisposable
{
    private readonly ActivitySource activitySource;
    private readonly Meter meter;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiTelemetryService"/> class.
    /// </summary>
    public ApiTelemetryService()
    {
        this.activitySource = new ActivitySource(TelemetryConstants.ApiActivitySource);
        this.meter = new Meter(TelemetryConstants.TenancyMeter, TelemetryConstants.ServiceVersion);

        // Initialize metrics
        this.TenantOperationsCounter = this.meter.CreateCounter<long>(
            "tenant_operations_total",
            "operations",
            "Total number of tenant operations");

        this.TenantOperationDuration = this.meter.CreateHistogram<double>(
            "tenant_operation_duration_milliseconds",
            "ms",
            "Duration of tenant operations in milliseconds");

        this.CacheHitsCounter = this.meter.CreateCounter<long>(
            "tenant_cache_hits_total",
            "hits",
            "Total number of tenant cache hits");

        this.CacheMissesCounter = this.meter.CreateCounter<long>(
            "tenant_cache_misses_total",
            "misses",
            "Total number of tenant cache misses");
    }

    /// <summary>
    /// Gets the counter for tenant operations.
    /// </summary>
    public Counter<long> TenantOperationsCounter { get; }

    /// <summary>
    /// Gets the histogram for tenant operation durations.
    /// </summary>
    public Histogram<double> TenantOperationDuration { get; }

    /// <summary>
    /// Gets the counter for cache hits.
    /// </summary>
    public Counter<long> CacheHitsCounter { get; }

    /// <summary>
    /// Gets the counter for cache misses.
    /// </summary>
    public Counter<long> CacheMissesCounter { get; }

    /// <summary>
    /// Starts a new activity for a tenant operation.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="operationType">The operation type.</param>
    /// <returns>The started activity or null if not sampled.</returns>
    public Activity? StartTenantOperation(string operationName, string? tenantId = null, string? operationType = null)
    {
        Activity? activity = this.activitySource.StartActivity(operationName);
        if (activity != null && !string.IsNullOrEmpty(tenantId))
        {
            activity.SetTenantOperationTags(operationType ?? "unknown", tenantId);
        }

        return activity;
    }

    /// <summary>
    /// Records the completion of a tenant operation with success status.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="duration">The operation duration in milliseconds.</param>
    public void RecordTenantOperationSuccess(Activity? activity, string operationType, double duration)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
        this.TenantOperationsCounter.Add(
            1,
            new KeyValuePair<string, object?>("operation", operationType),
            new KeyValuePair<string, object?>("status", "success"));
        this.TenantOperationDuration.Record(
            duration,
            new KeyValuePair<string, object?>("operation", operationType),
            new KeyValuePair<string, object?>("status", "success"));
    }

    /// <summary>
    /// Records the completion of a tenant operation with error status.
    /// </summary>
    /// <param name="activity">The activity to complete.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="duration">The operation duration in milliseconds.</param>
    /// <param name="exception">The exception that occurred.</param>
    public void RecordTenantOperationError(Activity? activity, string operationType, double duration, Exception exception)
    {
        activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        this.TenantOperationsCounter.Add(
            1,
            new KeyValuePair<string, object?>("operation", operationType),
            new KeyValuePair<string, object?>("status", "error"));
        this.TenantOperationDuration.Record(
            duration,
            new KeyValuePair<string, object?>("operation", operationType),
            new KeyValuePair<string, object?>("status", "error"));
    }

    /// <summary>
    /// Records a cache hit for tenant operations.
    /// </summary>
    /// <param name="operationType">The operation type that hit the cache.</param>
    public void RecordCacheHit(string operationType)
    {
        this.CacheHitsCounter.Add(1, new KeyValuePair<string, object?>("operation", operationType));
    }

    /// <summary>
    /// Records a cache miss for tenant operations.
    /// </summary>
    /// <param name="operationType">The operation type that missed the cache.</param>
    public void RecordCacheMiss(string operationType)
    {
        this.CacheMissesCounter.Add(1, new KeyValuePair<string, object?>("operation", operationType));
    }

    /// <summary>
    /// Disposes the telemetry service.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the telemetry service.
    /// </summary>
    /// <param name="disposing">Whether disposal is happening.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                this.activitySource?.Dispose();
                this.meter?.Dispose();
            }

            this.disposed = true;
        }
    }
}