// <copyright file="ApplicationInsightsDependencyFilter.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Telemetry;

using Marain.Tenancy.Shared.Telemetry;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Telemetry processor that filters out Application Insights dependency calls to prevent recursive logging.
/// </summary>
/// <param name="next">The next telemetry processor in the pipeline.</param>
/// <param name="configuration">The configuration instance to extract connection string from.</param>
public sealed class ApplicationInsightsDependencyFilter(ITelemetryProcessor next, IConfiguration configuration) : ITelemetryProcessor
{
    private readonly ITelemetryProcessor next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ApplicationInsightsEndpointChecker endpointChecker = ApplicationInsightsEndpointChecker.FromConfiguration(configuration);

    /// <summary>
    /// Processes telemetry items, filtering out Application Insights dependency calls.
    /// </summary>
    /// <param name="item">The telemetry item to process.</param>
    public void Process(ITelemetry item)
    {
        if (item is DependencyTelemetry dependency &&
            dependency.Type == "Http" &&
            this.endpointChecker.IsApplicationInsightsDependency(dependency.Target, dependency.Name))
        {
            // Skip Application Insights dependency calls to prevent recursive logging
            return;
        }

        this.next.Process(item);
    }
}