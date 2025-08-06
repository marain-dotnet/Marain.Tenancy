// <copyright file="TenancyClientBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Bindings;

using System;
using System.Collections.Generic;
using System.Net.Http;
using Corvus.Testing.ReqnRoll;
using Marain.Tenancy.Specs.Helpers;
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
    [BeforeFeature("@useTenancyClient", Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
    public static void SetupFeature(FeatureContext featureContext)
    {
        ContainerBindings.ConfigureServices(
            featureContext,
            serviceCollection =>
            {
                serviceCollection.AddUnauthenticatedTenancyClient(
                    MinimalApiWebApplicationFactory.Current.Server.BaseAddress.ToString(),
                    MinimalApiWebApplicationFactory.Current.Server.CreateHandler());
            });
    }
}