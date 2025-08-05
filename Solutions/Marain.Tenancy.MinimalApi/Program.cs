// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using Corvus.Storage.Azure.BlobStorage;
using Marain.Tenancy.MinimalApi.Extensions;
using Marain.Tenancy.MinimalApi.Models;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add environment variables configuration
builder.Configuration.AddEnvironmentVariables();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add tenancy minimal API services
builder.AddTenancyMinimalApi();

// Get cache settings
CacheControlConfiguration cacheControlConfiguration = builder.Configuration
    .GetSection("TenantCacheConfiguration")
    .Get<CacheControlConfiguration>()
    ?? new CacheControlConfiguration
    {
        GetTenantResponseCacheDurationSeconds = 0,
    };

builder.Services.AddSingleton(cacheControlConfiguration);

// Configure blob storage for tenant persistence
BlobContainerConfiguration? rootStorageConfiguration = builder.Configuration
    .GetSection("RootBlobStorageConfiguration")
    .Get<BlobContainerConfiguration>();

if (rootStorageConfiguration is not null)
{
    builder.Services.AddTenantStoreOnAzureBlobStorage(rootStorageConfiguration);
}

// Configure HTTPS redirection and HSTS in production
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });
}

// Add health checks
builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tenancy Service v1");
        c.RoutePrefix = "swagger-ui"; // Serve Swagger UI at /swagger-ui
    });
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseExceptionHandler();

// Add health check endpoint
app.MapHealthChecks("/health");

// Map custom /swagger endpoint to serve JSON directly
if (app.Environment.IsDevelopment())
{
    app.MapGet("/swagger", async (HttpContext context, IServiceProvider serviceProvider) =>
    {
        ISwaggerProvider swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();
        OpenApiDocument swagger = swaggerProvider.GetSwagger("v1");

        using var stringWriter = new StringWriter();
        swagger.SerializeAsV3(new OpenApiJsonWriter(stringWriter));
        string json = stringWriter.ToString();

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);
    });
}

// Map tenancy endpoints
app.MapTenancyEndpoints();

app.Run();

/// <summary>
/// Program class made available to tests.
/// </summary>
public partial class Program
{
}