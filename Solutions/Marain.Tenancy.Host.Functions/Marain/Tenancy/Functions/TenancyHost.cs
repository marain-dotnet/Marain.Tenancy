// <copyright file="TenancyHost.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Functions;

using System.Threading.Tasks;
using Marain.Tenancy.MinimalApi.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// The host for the tenancy services using Minimal APIs.
/// </summary>
public sealed class TenancyHost
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<TenancyHost> logger;
    private readonly RequestDelegate requestDelegate;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenancyHost"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger.</param>
    public TenancyHost(IServiceProvider serviceProvider, ILogger<TenancyHost> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.requestDelegate = FunctionsHostingExtensions.CreateTenancyRequestDelegate(serviceProvider);
    }

    /// <summary>
    /// Azure Functions entry point for all HTTP requests.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="executionContext">The function execution context.</param>
    /// <returns>An action result from processing the request.</returns>
    [FunctionName("TenancyHost-MinimalApi")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "patch", "post", "put", "delete", Route = "{*path}")]
        HttpRequest req, 
        ExecutionContext executionContext)
    {
        try
        {
            this.logger.LogInformation("Processing {Method} request to {Path}", req.Method, req.Path);

            // Create HttpContext for Minimal API processing
            var httpContext = FunctionsHostingExtensions.CreateHttpContextFromRequest(req, this.serviceProvider);
            
            // Store execution context for potential use in handlers
            httpContext.Items["ExecutionContext"] = executionContext;

            // Process the request through our Minimal API pipeline
            await this.requestDelegate(httpContext);

            // Convert response to IActionResult
            var response = httpContext.Response;
            
            if (response.StatusCode >= 200 && response.StatusCode < 300)
            {
                if (response.Body.Length > 0)
                {
                    response.Body.Position = 0;
                    using var reader = new StreamReader(response.Body);
                    var content = await reader.ReadToEndAsync();
                    return new ContentResult
                    {
                        Content = content,
                        StatusCode = response.StatusCode,
                        ContentType = response.ContentType ?? "application/json"
                    };
                }
                
                return new StatusCodeResult(response.StatusCode);
            }

            return new StatusCodeResult(response.StatusCode);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error processing request {Method} {Path}", req.Method, req.Path);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}