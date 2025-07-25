// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using Corvus.Storage.Azure.BlobStorage;
using Marain.Tenancy.MinimalApi.Extensions;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables configuration
builder.Configuration.AddEnvironmentVariables();

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

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseExceptionHandler();

// Map tenancy endpoints
app.MapTenancyEndpoints();

app.Run();