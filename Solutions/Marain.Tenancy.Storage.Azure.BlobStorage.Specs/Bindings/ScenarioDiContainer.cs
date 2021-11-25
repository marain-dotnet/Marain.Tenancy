// <copyright file="ScenarioDiContainer.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings
{
    using System;

    using Corvus.Storage.Azure.BlobStorage;
    using Corvus.Tenancy;
    using Corvus.Testing.SpecFlow;

    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.MultiMode;

    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using NUnit.Framework.Internal;

    using TechTalk.SpecFlow;

    /// <summary>
    /// Sets up the DI container used by tests with the <c>@withBlobStorageTenantProvider</c> tag.
    /// </summary>
    [Binding]
    public class ScenarioDiContainer
    {
        private readonly ScenarioContext scenarioContext;
        private ITenantStore? tenantStore;
        private IServiceProvider? serviceProvider;
        private BlobContainerConfiguration? rootBlobStorageConfiguration;

        /// <summary>
        /// Creates a <see cref="ScenarioDiContainer"/>.
        /// </summary>
        /// <param name="scenarioContext">The scenario context from SpecFlow.</param>
        public ScenarioDiContainer(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;

            this.SetupMode = TestExecutionContext.CurrentContext.TestObject switch
            {
                IMultiModeTest<SetupModes> multiModeTest => multiModeTest.TestType,
                _ => SetupModes.ViaApi,
            };

            this.Configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("local.settings.json", true, true)
                .Build();
        }

        /// <summary>
        /// Gets the configuration root.
        /// </summary>
        public IConfiguration Configuration { get; }

        public IServiceProvider ServiceProvider => this.serviceProvider
            ?? throw new InvalidOperationException("Not available until DI initialization complete");

        /// <summary>
        /// Gets the blob storage configuration to use for the root tenant.
        /// </summary>
        public BlobContainerConfiguration RootBlobStorageConfiguration => this.rootBlobStorageConfiguration
            ?? throw new InvalidOperationException("Not available until DI initialization complete");

        /// <summary>
        /// Gets the mode to use when setting up the containers in tests.
        /// </summary>
        public SetupModes SetupMode { get; }

        /// <summary>
        /// Gets the <c>ITenantStore</c> implementation to test.
        /// </summary>
        public ITenantStore TenantStore => this.tenantStore
            ?? throw new InvalidOperationException("Not available until DI initialization complete");

        /// <summary>
        /// Configure the DI container. Called by SpecFlow.
        /// </summary>
        [BeforeScenario("@withBlobStorageTenantProvider", Order = ContainerBeforeScenarioOrder.PopulateServiceCollection)]
        public void ConfigureServices()
        {
            ContainerBindings.ConfigureServices(this.scenarioContext, services =>
            {
                this.rootBlobStorageConfiguration = this.Configuration
                    .GetSection("RootBlobStorageConfiguration")
                    .Get<BlobContainerConfiguration>();
                services.AddTenantStoreOnAzureBlobStorage(this.rootBlobStorageConfiguration);
            });
        }

        /// <summary>
        /// Retrieve initialization services to the DI container. Called by SpecFlow.
        /// </summary>
        [BeforeScenario("@withBlobStorageTenantProvider", Order = ContainerBeforeScenarioOrder.ServiceProviderAvailable)]
        public void InitializeServiceProperties()
        {
            this.serviceProvider = ContainerBindings.GetServiceProvider(this.scenarioContext);
            this.tenantStore = this.serviceProvider.GetRequiredService<ITenantStore>();
        }
    }
}