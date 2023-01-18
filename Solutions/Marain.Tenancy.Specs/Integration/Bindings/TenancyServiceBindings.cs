// <copyright file="TenancyServiceBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings
{
    using System;

    using Corvus.Storage.Azure.BlobStorage;
    using Corvus.Testing.SpecFlow;

    using Marain.Tenancy.Client;

    using Menes;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using TechTalk.SpecFlow;

    /// <summary>
    /// Bindings for the integration tests for <see cref="TenancyService"/>.
    /// </summary>
    [Binding]
    public static class TenancyServiceBindings
    {
        /// <summary>
        /// Configures the DI container before tests start.
        /// </summary>
        /// <param name="featureContext">The SpecFlow test context.</param>
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

                        serviceCollection.AddSingleton(sp => sp.GetRequiredService<IConfiguration>().Get<TenancyClientOptions>()
                            ?? throw new InvalidOperationException("TenancyClientOptions missing from Configuration."));

                        BlobContainerConfiguration rootStorageConfiguration = config
                            .GetSection("RootBlobStorageConfiguration")
                            .Get<BlobContainerConfiguration>()
                                ?? throw new InvalidOperationException("RootBlobStorageConfiguration:BlobContainerConfiguration missing from Configuration.");

                        serviceCollection.AddTenantStoreOnAzureBlobStorage(rootStorageConfiguration);

                        serviceCollection.AddSingleton<SimpleOpenApiContext>();
                        serviceCollection.AddTenancyApiWithOpenApiActionResultHosting(ConfigureOpenApiHost);
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
}