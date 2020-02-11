// <copyright file="OperationsControlApiAndTasksBindings.cs" company="Endjin">
// Copyright (c) Endjin. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings
{
    using System.Collections.Generic;
    using Corvus.Azure.Storage.Tenancy;
    using Corvus.SpecFlow.Extensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using TechTalk.SpecFlow;

    /// <summary>
    /// Bindings for the integration tests for <see cref="TenancyService"/>.
    /// </summary>
    [Binding]
    public static class TenancyApiBindings
    {
        /// <summary>
        /// Configures the DI container before tests start.
        /// </summary>
        /// <param name="featureContext">The SpecFlow test context.</param>
        [BeforeFeature("@tenancyApi", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
        public static void SetupFeature(FeatureContext featureContext)
        {
            ContainerBindings.ConfigureServices(
                featureContext,
                serviceCollection =>
                {
                    var configData = new Dictionary<string, string>
                    {
                    };
                    IConfigurationRoot config = new ConfigurationBuilder()
                        .AddInMemoryCollection(configData)
                        .AddEnvironmentVariables()
                        .AddJsonFile("local.settings.json", true, true)
                        .Build();
                    //serviceCollection.AddSingleton(config);
                    serviceCollection.AddSingleton(config.Get<TenantCloudBlobContainerFactoryOptions>());
                    serviceCollection.AddTenancyApiOnBlobStorage();
                });
        }
    }
}
