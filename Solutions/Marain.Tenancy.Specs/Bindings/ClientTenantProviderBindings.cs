// <copyright file="ClientTenantProviderBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Bindings;

using Corvus.Testing.ReqnRoll;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

/// <summary>
/// Bindings for the integration tests for tenancy services.
/// </summary>
[Binding]
public static class ClientTenantProviderBindings
{
    /// <summary>
    /// Configures the DI container before tests start.
    /// </summary>
    /// <param name="featureContext">The Reqnroll test context.</param>
    [BeforeFeature("@useClientTenantProvider", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
    public static void SetupFeature(FeatureContext featureContext)
    {
        ContainerBindings.ConfigureServices(
            featureContext,
            serviceCollection =>
            {
                serviceCollection.AddTenantProviderServiceClient();
            });
    }
}