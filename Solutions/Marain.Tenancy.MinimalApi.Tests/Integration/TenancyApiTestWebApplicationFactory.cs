// <copyright file="TenancyApiTestWebApplicationFactory.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Tests.Integration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Test web application factory for integration testing the Tenancy Minimal API.
/// </summary>
public sealed class TenancyApiTestWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Configures the web host builder for testing.
    /// </summary>
    /// <param name="builder">The web host builder.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.test.json", optional: false);
        });

        builder.ConfigureServices(services =>
        {
            // Replace any services for testing if needed
            // For example, we could override the tenant service with a test implementation
        });

        builder.UseEnvironment("Testing");
    }
}