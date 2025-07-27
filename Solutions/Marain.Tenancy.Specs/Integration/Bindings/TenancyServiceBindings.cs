// <copyright file="TenancyServiceBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings;

using System;

using Corvus.Storage.Azure.BlobStorage;
using Corvus.Testing.ReqnRoll;

using Marain.Tenancy.Client;
using Marain.Tenancy.MinimalApi.Extensions;

using Menes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Reqnroll;

/// <summary>
/// Bindings for the integration tests for <see cref="TenancyService"/>.
/// </summary>
[Binding]
public static class TenancyServiceBindings
{
    /// <summary>
    /// Configures the DI container before tests start.
    /// </summary>
    /// <param name="featureContext">The Reqnroll test context.</param>
    [BeforeFeature("withTenancyClient", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
    public static void SetupFeature(FeatureContext featureContext)
    {
        ContainerBindings.ConfigureServices(
            featureContext,
            serviceCollection =>
            {
                if (FunctionBindings.TestHostMode == MultiHost.TestHostModes.DirectInvocation)
                {
                    IConfiguration config = new ConfigurationBuilder()
                        .AddEnvironmentVariables()
                        .AddJsonFile("local.settings.json", true, true)
                        .Build();
                    serviceCollection.AddSingleton(config);

                    serviceCollection.AddSingleton(sp => sp.GetRequiredService<IConfiguration>().Get<TenancyClientOptions>());

                    BlobContainerConfiguration rootStorageConfiguration = config
                        .GetSection("RootBlobStorageConfiguration")
                        .Get<BlobContainerConfiguration>();

                    serviceCollection.AddTenantStoreOnAzureBlobStorage(rootStorageConfiguration);

                    serviceCollection.AddSingleton<SimpleOpenApiContext>();
                    serviceCollection.AddTenancyApiWithOpenApiActionResultHosting(ConfigureOpenApiHost);
                }
                else if (FunctionBindings.TestHostMode == MultiHost.TestHostModes.InProcessMinimalApi)
                {
                    IConfiguration config = new ConfigurationBuilder()
                        .AddEnvironmentVariables()
                        .AddJsonFile("local.settings.json", true, true)
                        .Build();
                    serviceCollection.AddSingleton(config);

                    BlobContainerConfiguration rootStorageConfiguration = config
                        .GetSection("RootBlobStorageConfiguration")
                        .Get<BlobContainerConfiguration>();

                    serviceCollection.AddTenantStoreOnAzureBlobStorage(rootStorageConfiguration);
                }
            });
    }

    private static void ConfigureOpenApiHost(IOpenApiHostConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config), "AddTenancyApi callback: config");
        }

        if (config.Documents == null)
        {
            throw new ArgumentNullException(nameof(config.Documents), "AddTenancyApi callback: config.Documents");
        }

        config.Documents.AddSwaggerEndpoint();
    }
}