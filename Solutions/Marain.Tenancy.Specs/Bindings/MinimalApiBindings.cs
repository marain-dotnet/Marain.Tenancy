// <copyright file="MinimalApiBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Bindings;

using System.Net.Http;
using Corvus.Testing.ReqnRoll;
using Marain.Tenancy.Specs.Helpers;
using Reqnroll;
using Reqnroll.BoDi;

/// <summary>
/// Provides function initialisation for tests that require endpoints to be available.
/// </summary>
[Binding]
public static class MinimalApiBindings
{
    /// <summary>
    /// Runs the public API function.
    /// </summary>
    /// <param name="featureContext">The current feature context.</param>
    /// <param name="specFlowDiContainer">Specflow's dependency injection container.</param>
    [BeforeFeature("useTenancyApi", Order = ContainerBeforeFeatureOrder.ServiceProviderAvailable)]
    public static void RunPublicApiFunction(
        FeatureContext featureContext,
        IObjectContainer specFlowDiContainer)
    {
        HttpClient client = MinimalApiWebApplicationFactory.Current.Client;
        specFlowDiContainer.RegisterInstanceAs(client);
    }
}