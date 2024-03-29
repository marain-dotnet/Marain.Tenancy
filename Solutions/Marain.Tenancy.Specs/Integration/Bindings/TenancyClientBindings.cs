﻿// <copyright file="TenancyClientBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings
{
    using System.Collections.Generic;
    using System.Linq;
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
    public static class TenancyClientBindings
    {
        /// <summary>
        /// Configures the DI container before tests start.
        /// </summary>
        /// <param name="featureContext">The SpecFlow test context.</param>
        [BeforeFeature("@withTenancyClient", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
        public static void SetupFeature(FeatureContext featureContext)
        {
            ContainerBindings.ConfigureServices(
                featureContext,
                serviceCollection =>
                {
                    if (FunctionBindings.TestHostMode != MultiHost.TestHostModes.DirectInvocation)
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

                        serviceCollection.AddJsonNetSerializerSettingsProvider();
                        serviceCollection.AddJsonNetPropertyBag();
                        serviceCollection.AddJsonNetCultureInfoConverter();
                        serviceCollection.AddJsonNetDateTimeOffsetToIso8601AndUnixTimeConverter();
                        serviceCollection.AddSingleton<JsonConverter>(new StringEnumConverter(new CamelCaseNamingStrategy()));

                        serviceCollection.AddSingleton(sp => sp.GetRequiredService<IConfiguration>().Get<TenancyClientOptions>());

                        bool enableCaching = !featureContext.FeatureInfo.Tags.Contains("disableTenantCaching");

                        serviceCollection.AddTenantProviderServiceClient(enableCaching);
                    }
                });
        }
    }
}