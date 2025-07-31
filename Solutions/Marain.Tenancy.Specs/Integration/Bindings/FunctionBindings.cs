// <copyright file="FunctionBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings;

using System;
using System.Linq;
using System.Threading.Tasks;
using Reqnroll.BoDi;
using Corvus.Extensions.Json;
using Corvus.Testing.AzureFunctions;
using Corvus.Testing.AzureFunctions.ReqnRoll;
using Corvus.Testing.ReqnRoll;
using Marain.Tenancy.Specs.MultiHost;
using Menes;
using Menes.Testing.AspNetCoreSelfHosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework.Internal;
using Reqnroll;

/// <summary>
/// Provides function initialisation for tests that require endpoints to be available.
/// </summary>
[Binding]
public static class FunctionBindings
{
    /// <summary>
    /// The port on which we host the function.
    /// </summary>
    public const int TenancyApiPort = 7071;

    private static readonly string TenancyApiBaseUriText = $"http://localhost:{TenancyApiPort}";

    public static Uri TenancyApiBaseUri { get; } = new(TenancyApiBaseUriText);

    public static TestHostModes TestHostMode => TestExecutionContext.CurrentContext.TestObject switch
    {
        IMultiModeTest<TestHostModes> multiModeTest => multiModeTest.TestType,
        _ => TestHostModes.InProcessMinimalApi,
    };

    /// <summary>
    /// Runs the public API function.
    /// </summary>
    /// <param name="featureContext">The current feature context.</param>
    /// <param name="specFlowDiContainer">Specflow's dependency injection container.</param>
    /// <returns>A task that completes when the functions have been started.</returns>
    [BeforeFeature("useTenancyFunction", Order = ContainerBeforeFeatureOrder.ServiceProviderAvailable)]
    public static async Task RunPublicApiFunction(
        FeatureContext featureContext,
        IObjectContainer specFlowDiContainer)
    {
        IConfiguration config = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<IConfiguration>();
        IServiceProvider serviceProvider = ContainerBindings.GetServiceProvider(featureContext);

        switch (TestHostMode)
        {
            case TestHostModes.InProcessMinimalApi:
                // For MinimalApi testing, we'll create the WebApplicationFactory in the service wrapper
                break;

            case TestHostModes.TenancyClient:
                // For client testing, we need to start a MinimalApi instance
                break;
        }

        ITestableTenancyService serviceWrapper = TestHostMode switch
        {
            TestHostModes.InProcessMinimalApi => CreateMinimalApiTestableTenancyService(),
            TestHostModes.TenancyClient => new ClientTestableTenancyService(
                TenancyApiBaseUriText,
                serviceProvider.GetRequiredService<IJsonSerializerSettingsProvider>().Instance),
            _ => CreateMinimalApiTestableTenancyService(), // Default to MinimalApi
        };

        specFlowDiContainer.RegisterInstanceAs(serviceWrapper);
    }

    /// <summary>
    /// Tear down the running functions instances for the feature.
    /// </summary>
    /// <param name="featureContext">The current scenario context.</param>
    [AfterFeature(Order = 100)]
    public static void TeardownFunctionsAfterScenario(FeatureContext featureContext)
    {
        if (featureContext.TryGetValue(out FunctionsController functionsController))
        {
            featureContext.RunAndStoreExceptionsAsync(() => functionsController.TeardownFunctionsAsync());
        }
    }

    private static ITestableTenancyService CreateMinimalApiTestableTenancyService()
    {
        return new MinimalApiTestableTenancyService();
    }
}