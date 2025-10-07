// <copyright file="ApplicationInsightsEndpointChecker.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Shared.Telemetry;

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Utility class for checking if URIs are Application Insights endpoints.
/// Parses the connection string once during initialization for efficiency.
/// </summary>
public sealed class ApplicationInsightsEndpointChecker
{
    private readonly HashSet<string> applicationInsightsEndpoints;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationInsightsEndpointChecker"/> class.
    /// </summary>
    /// <param name="connectionString">The Application Insights connection string to parse.</param>
    public ApplicationInsightsEndpointChecker(string? connectionString)
    {
        this.applicationInsightsEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        this.ParseApplicationInsightsEndpoints(connectionString);
    }

    /// <summary>
    /// Creates an instance from the configuration's Application Insights connection string.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>A new <see cref="ApplicationInsightsEndpointChecker"/> instance.</returns>
    public static ApplicationInsightsEndpointChecker FromConfiguration(IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("ApplicationInsights");
        return new ApplicationInsightsEndpointChecker(connectionString);
    }

    /// <summary>
    /// Checks if the given URI string is an Application Insights endpoint.
    /// </summary>
    /// <param name="requestUri">The request URI to check.</param>
    /// <returns>True if the URI is an Application Insights endpoint, false otherwise.</returns>
    public bool IsApplicationInsightsEndpoint(string requestUri)
    {
        if (string.IsNullOrEmpty(requestUri))
        {
            return false;
        }

        // Check for common Application Insights telemetry endpoint paths
        if (requestUri.Contains("/v2/track", StringComparison.OrdinalIgnoreCase) ||
            requestUri.Contains("/v2.1/track", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check against the parsed Application Insights endpoints
        foreach (string endpoint in this.applicationInsightsEndpoints)
        {
            if (requestUri.Contains(endpoint, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the given dependency target or name is an Application Insights endpoint.
    /// </summary>
    /// <param name="target">The dependency target (hostname).</param>
    /// <param name="name">The dependency name (URL or operation name).</param>
    /// <returns>True if the dependency is to an Application Insights endpoint, false otherwise.</returns>
    public bool IsApplicationInsightsDependency(string? target, string? name)
    {
        // Check target hostname
        if (!string.IsNullOrEmpty(target))
        {
            foreach (string endpoint in this.applicationInsightsEndpoints)
            {
                if (target.Contains(endpoint, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        // Check name (which often contains the full URL)
        if (!string.IsNullOrEmpty(name))
        {
            ////if (name.Contains("/v2/track", StringComparison.OrdinalIgnoreCase) ||
            ////    name.Contains("/v2.1/track", StringComparison.OrdinalIgnoreCase))
            ////{
            ////    return true;
            ////}

            foreach (string endpoint in this.applicationInsightsEndpoints)
            {
                if (name.Contains(endpoint, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ParseApplicationInsightsEndpoints(string? connectionString)
    {
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
                        this.applicationInsightsEndpoints.Add(uri.Host);
                    }
                }
            }
        }

        // Add common fallback endpoints in case IngestionEndpoint is not specified
        this.applicationInsightsEndpoints.Add("dc.applicationinsights.azure.com");
        this.applicationInsightsEndpoints.Add("dc.services.visualstudio.com");
        this.applicationInsightsEndpoints.Add("rt.services.visualstudio.com");

        // Add pattern for regional Application Insights endpoints
        // Note: We store the pattern without the subdomain since we use Contains() for matching
        this.applicationInsightsEndpoints.Add(".in.applicationinsights.azure.com");
    }
}