// <copyright file="TenancyClientBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings
{
    using System.Collections.Generic;
    using System.Linq;
    using Corvus.Storage.Azure.BlobStorage;
    using Corvus.Testing.SpecFlow;
    using Marain.Tenancy.Client;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

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
        /// 
        [BeforeFeature("directInvocation", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
        [Scope(Tag = "directInvocation")]
        public static void SetupFeature(FeatureContext featureContext)
        {
            ContainerBindings.ConfigureServices(
                featureContext,
                serviceCollection =>
                {
                    var configData = new Dictionary<string, string>
                    {
                        { "TenancyServiceBaseUri", "http://localhost:7071" },
                    };
                    IConfiguration config = new ConfigurationBuilder()
                        .AddInMemoryCollection(configData)
                        .AddEnvironmentVariables()
                        .AddJsonFile("local.settings.json", true, true)
                        .Build();
                    serviceCollection.AddSingleton(config);

                    serviceCollection.AddSingleton(sp => sp.GetRequiredService<IConfiguration>().Get<TenancyClientOptions>());

                    //bool enableCaching = !featureContext.FeatureInfo.Tags.Contains("disableTenantCaching");

                    //serviceCollection.AddTenantProviderServiceClient(enableCaching);

                    BlobContainerConfiguration rootStorageConfiguration = config
                                            .GetSection("RootBlobStorageConfiguration")
                                            .Get<BlobContainerConfiguration>();
                    serviceCollection.AddSingleton(rootStorageConfiguration);
                    serviceCollection.AddTenantStoreOnAzureBlobStorage(rootStorageConfiguration);
                    serviceCollection.AddTenancyApiWithOpenApiActionResultHosting();
                });
        }
    }
}
