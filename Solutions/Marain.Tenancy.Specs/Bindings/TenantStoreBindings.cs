// <copyright file="TenantStoreBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Bindings;

using Corvus.Storage.Azure.BlobStorage;
using Corvus.Testing.ReqnRoll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

/// <summary>
/// Bindings for the integration tests for tenancy services.
/// </summary>
[Binding]
public static class TenantStoreBindings
{
    /// <summary>
    /// Configures the DI container before tests start.
    /// </summary>
    /// <param name="featureContext">The Reqnroll test context.</param>
    [BeforeFeature("withTenancyClient", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
    public static void SetupFeature(FeatureContext featureContext)
    {
        ////ContainerBindings.ConfigureServices(
        ////    featureContext,
        ////    serviceCollection =>
        ////    {
        ////        IConfiguration config = new ConfigurationBuilder()
        ////            .AddEnvironmentVariables()
        ////            .AddJsonFile("local.settings.json", true, true)
        ////            .Build();
        ////        serviceCollection.AddSingleton(config);

        ////        Configure blob storage for tenant persistence

        ////       BlobContainerConfiguration ? rootStorageConfiguration = config
        ////           .GetSection("RootBlobStorageConfiguration")
        ////           .Get<BlobContainerConfiguration>();

        ////        if (rootStorageConfiguration is not null)
        ////            {
        ////                serviceCollection.AddTenantStoreOnAzureBlobStorage(rootStorageConfiguration);
        ////            }
        ////    });
    }
}