// <copyright file="Startup.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.WebJobs.Hosting.WebJobsStartup(typeof(Marain.Tenancy.ControlHost.Startup))]

namespace Marain.Tenancy.ControlHost;

using Azure.Identity;
using Corvus.Identity.ClientAuthentication.Azure;
using Corvus.Storage.Azure.BlobStorage;
using Marain.Tenancy.MinimalApi.Hosting;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Startup code for the Azure Function using Minimal APIs.
/// </summary>
public sealed class Startup : FunctionsStartup
{
    /// <inheritdoc/>
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.GetContext().Configuration;

        services.AddApplicationInsightsInstrumentationTelemetry();
        services.AddLogging();

        // Add Minimal API services configured for Functions
        builder.AddTenancyMinimalApiForFunctions();

        // Configure blob storage for tenant persistence
        var rootStorageConfiguration = configuration
            .GetSection("RootBlobStorageConfiguration")
            .Get<BlobContainerConfiguration>();

        if (rootStorageConfiguration is not null)
        {
            // TODO: Re-enable when storage dependencies are resolved
            // services.AddTenantStoreOnAzureBlobStorage(rootStorageConfiguration);
            
            // Configure Azure identity for blob storage access
            if (rootStorageConfiguration.AccessKeyInKeyVault?.VaultClientIdentity is ClientIdentityConfiguration idConfig)
            {
                services.AddServiceIdentityAzureTokenCredentialSourceFromClientIdentityConfiguration(idConfig);
            }
            else
            {
                services.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(new DefaultAzureCredential());
            }
        }
    }
}