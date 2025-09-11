// <copyright file="ApplicationInsightsDependencyFilter.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Telemetry;

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
    private readonly HashSet<string> applicationInsightsEndpoints = InitializeEndpoints(configuration);

    /// <summary>
    /// Processes telemetry items, filtering out Application Insights dependency calls.
    /// </summary>
    /// <param name="item">The telemetry item to process.</param>
    public void Process(ITelemetry item)
    {
        if (item is DependencyTelemetry dependency && IsApplicationInsightsDependency(dependency, this.applicationInsightsEndpoints))
        {
            // Skip Application Insights dependency calls to prevent recursive logging
            return;
        }

        this.next.Process(item);
    }

    private static HashSet<string> InitializeEndpoints(IConfiguration configuration)
    {
        HashSet<string> endpoints = new(StringComparer.OrdinalIgnoreCase);
        string? connectionString = configuration.GetConnectionString("ApplicationInsights");

        if (!string.IsNullOrEmpty(connectionString))
        {
            // Parse the connection string to extract IngestionEndpoint
            string[] parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                string trimmedPart = part.Trim();
                if (trimmedPart.StartsWith("IngestionEndpoint=", StringComparison.OrdinalIgnoreCase))
                {
                    string endpoint = trimmedPart.Substring("IngestionEndpoint=".Length);
                    if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri))
                    {
                        endpoints.Add(uri.Host);
                    }
                }
            }
        }

        // Add common fallback endpoints in case IngestionEndpoint is not specified
        endpoints.Add("dc.applicationinsights.azure.com");
        endpoints.Add("dc.services.visualstudio.com");
        endpoints.Add("rt.services.visualstudio.com");

        return endpoints;
    }

    private static bool IsApplicationInsightsDependency(DependencyTelemetry dependency, HashSet<string> endpoints)
    {
        if (dependency.Type != "Http")
        {
            return false;
        }

        // Check if the target or name contains any of the Application Insights endpoints
        foreach (string endpoint in endpoints)
        {
            if (dependency.Target?.Contains(endpoint, StringComparison.OrdinalIgnoreCase) == true ||
                dependency.Name?.Contains(endpoint, StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        return false;
    }
}