// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using Corvus.Storage.Azure.BlobStorage;
using Marain.Tenancy.MinimalApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables configuration
builder.Configuration.AddEnvironmentVariables();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add tenancy minimal API services
builder.AddTenancyMinimalApi();

// Configure blob storage for tenant persistence
var rootStorageConfiguration = builder.Configuration
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

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tenancy Service v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
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

// Map tenancy endpoints
app.MapTenancyEndpoints();

app.Run();

/// <summary>
/// Program class made available to tests.
/// </summary>
public partial class Program
{
}