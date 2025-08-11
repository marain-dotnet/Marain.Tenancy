// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using Corvus.Storage.Azure.BlobStorage;
using Marain.Tenancy.MinimalApi.Extensions;
using Marain.Tenancy.MinimalApi.Models;

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

builder.ConfigureUnifiedJsonSerialization();

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

// Add HTTP logging for request/response body logging in development
// This logs all HTTP request/response details including bodies up to 4KB each
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpLogging(options =>
    {
        options.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
        options.RequestBodyLogLimit = 4096;
        options.ResponseBodyLogLimit = 4096;
        options.CombineLogs = true; // Single log entry per request
    });
}

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

// Add HTTP logging middleware in development
if (app.Environment.IsDevelopment())
{
    app.UseHttpLogging();
}

// Add health check endpoint
app.MapHealthChecks("/health");
app.MapCustomSwaggerEndpoint();
app.MapSerializationDemoEndpoint();
app.MapTenancyEndpoints();

app.Run();

/// <summary>
/// Program class made available to tests.
/// </summary>
public partial class Program
{
}