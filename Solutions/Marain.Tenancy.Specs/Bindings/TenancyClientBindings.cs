// <copyright file="TenancyClientBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Bindings;

using System.Collections.Generic;
using Corvus.Testing.ReqnRoll;
using Marain.Tenancy.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

/// <summary>
/// Bindings for the integration tests for tenancy client.
/// </summary>
[Binding]
public static class TenancyClientBindings
{
    /// <summary>
    /// Configures the DI container before tests start.
    /// </summary>
    /// <param name="featureContext">The Reqnroll test context.</param>
    [BeforeFeature("@withTenancyClient", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
    public static void SetupFeature(FeatureContext featureContext)
    {
        ////ContainerBindings.ConfigureServices(
        ////    featureContext,
        ////    serviceCollection =>
        ////    {
        ////        if (FunctionBindings.TestHostMode == MultiHost.TestHostModes.TenancyClient)
        ////        {
        ////            var configData = new Dictionary<string, string?>
        ////            {
        ////                { "TenancyServiceBaseUri", "http://localhost:7071" },
        ////            };
        ////            IConfiguration config = new ConfigurationBuilder()
        ////                .AddInMemoryCollection(configData)
        ////                .AddEnvironmentVariables()
        ////                .AddJsonFile("local.settings.json", true, true)
        ////                .Build();
        ////            serviceCollection.AddSingleton(config);

        ////            // Add the Tenancy client services
        ////            string? baseUri = config["TenancyServiceBaseUri"];
        ////            if (!string.IsNullOrEmpty(baseUri))
        ////            {
        ////                serviceCollection.AddUnauthenticatedTenancyClient(baseUri);
        ////            }
        ////        }
        ////    });
    }
}