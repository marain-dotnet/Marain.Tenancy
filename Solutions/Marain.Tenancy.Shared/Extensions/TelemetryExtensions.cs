// <copyright file="TelemetryExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Shared.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using Marain.Tenancy.Shared.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Instrumentation.Runtime;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Extension methods for telemetry configuration.
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Adds Marain Tenancy telemetry services to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="environment">The hosting environment (optional).</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMarainTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string? connectionString = configuration.GetConnectionString("ApplicationInsights");
        bool isDevelopment = environment?.IsDevelopment() ?? false;

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(TelemetryConstants.ServiceName, TelemetryConstants.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment?.EnvironmentName ?? "unknown",
                    ["service.instance.id"] = Environment.MachineName,
                }))
            .WithTracing(tracing =>
            {
                tracing.AddSource(TelemetryConstants.ApiActivitySource)
                       .AddSource(TelemetryConstants.CliActivitySource)
                       .AddSource(TelemetryConstants.ClientActivitySource)
                       .AddSource(TelemetryConstants.BusinessActivitySource)
                       .AddSource(TelemetryConstants.StorageActivitySource)
                       .AddHttpClientInstrumentation();

                if (!string.IsNullOrEmpty(connectionString))
                {
                    tracing.AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = connectionString;
                    });
                }
                else if (isDevelopment)
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter(TelemetryConstants.TenancyMeter)
                       .AddRuntimeInstrumentation();

                if (!string.IsNullOrEmpty(connectionString))
                {
                    metrics.AddAzureMonitorMetricExporter(options =>
                    {
                        options.ConnectionString = connectionString;
                    });
                }
                else if (isDevelopment)
                {
                    metrics.AddConsoleExporter();
                }
            });

        return services;
    }

    /// <summary>
    /// Sets standard tenant operation tags on an activity.
    /// </summary>
    /// <param name="activity">The activity to add tags to.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>The activity for chaining.</returns>
    public static Activity? SetTenantOperationTags(this Activity? activity, string operationType, string tenantId)
    {
        activity?.SetTag(TelemetryConstants.AttributeKeys.OperationType, operationType);
        activity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, tenantId);
        return activity;
    }

    /// <summary>
    /// Sets standard command operation tags on an activity.
    /// </summary>
    /// <param name="activity">The activity to add tags to.</param>
    /// <param name="commandType">The command type.</param>
    /// <param name="tenantId">The tenant ID (optional).</param>
    /// <returns>The activity for chaining.</returns>
    public static Activity? SetCommandOperationTags(this Activity? activity, string commandType, string? tenantId = null)
    {
        activity?.SetTag(TelemetryConstants.AttributeKeys.CommandType, commandType);
        if (!string.IsNullOrEmpty(tenantId))
        {
            activity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, tenantId);
        }

        return activity;
    }

    /// <summary>
    /// Sets standard storage operation tags on an activity.
    /// </summary>
    /// <param name="activity">The activity to add tags to.</param>
    /// <param name="storageOperation">The storage operation type.</param>
    /// <param name="tenantId">The tenant ID (optional).</param>
    /// <param name="containerName">The Azure container name (optional).</param>
    /// <returns>The activity for chaining.</returns>
    public static Activity? SetStorageOperationTags(this Activity? activity, string storageOperation, string? tenantId = null, string? containerName = null)
    {
        activity?.SetTag(TelemetryConstants.AttributeKeys.StorageOperation, storageOperation);
        if (!string.IsNullOrEmpty(tenantId))
        {
            activity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, tenantId);
        }

        if (!string.IsNullOrEmpty(containerName))
        {
            activity?.SetTag(TelemetryConstants.AttributeKeys.AzureContainer, containerName);
        }

        return activity;
    }
}