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
                       .AddHttpClientInstrumentation(options =>
                       {
                           // Filter out Application Insights dependency calls to prevent recursive logging
                           options.FilterHttpRequestMessage = (httpRequestMessage) =>
                           {
                               string? requestUri = httpRequestMessage.RequestUri?.ToString();
                               if (string.IsNullOrEmpty(requestUri))
                               {
                                   return true;
                               }

                               // Filter out calls to Application Insights ingestion endpoints
                               return !IsApplicationInsightsEndpoint(requestUri, connectionString);
                           };
                       });

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

    /// <summary>
    /// Checks if the given URI is an Application Insights endpoint.
    /// </summary>
    /// <param name="requestUri">The request URI to check.</param>
    /// <param name="connectionString">The Application Insights connection string.</param>
    /// <returns>True if the URI is an Application Insights endpoint, false otherwise.</returns>
    private static bool IsApplicationInsightsEndpoint(string requestUri, string? connectionString)
    {
        // Check for common Application Insights telemetry endpoints
        if (requestUri.Contains("/v2/track", StringComparison.OrdinalIgnoreCase) ||
            requestUri.Contains("/v2.1/track", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check against the configured ingestion endpoint
        if (!string.IsNullOrEmpty(connectionString))
        {
            string[] parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                if (trimmedPart.StartsWith("IngestionEndpoint=", StringComparison.OrdinalIgnoreCase))
                {
                    string endpoint = trimmedPart.Substring("IngestionEndpoint=".Length);
                    if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri) &&
                        requestUri.Contains(uri.Host, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }

        // Check against common Application Insights hostnames
        return requestUri.Contains("dc.applicationinsights.azure.com", StringComparison.OrdinalIgnoreCase) ||
               requestUri.Contains("dc.services.visualstudio.com", StringComparison.OrdinalIgnoreCase) ||
               requestUri.Contains("rt.services.visualstudio.com", StringComparison.OrdinalIgnoreCase) ||
               requestUri.Contains(".in.applicationinsights.azure.com", StringComparison.OrdinalIgnoreCase);
    }
}