// <copyright file="FunctionBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings
{
    using System;
    using System.Threading.Tasks;
    using Corvus.Testing.AzureFunctions;
    using Corvus.Testing.AzureFunctions.SpecFlow;
    using Corvus.Testing.SpecFlow;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    /// <summary>
    /// Provides function initialisation for tests that require endpoints to be available.
    /// </summary>
    [Binding]
    public static class FunctionBindings
    {
        public const int TenancyApiPort = 7071;

        public static readonly Uri TenancyApiBaseUri = new Uri($"http://localhost:{TenancyApiPort}");

        /// <summary>
        /// Runs the public API function.
        /// </summary>
        /// <param name="featureContext">The current feature context.</param>
        /// <returns>A task that completes when the functions have been started.</returns>
        [BeforeFeature("useTenancyFunction", Order = ContainerBeforeFeatureOrder.ServiceProviderAvailable)]
        public static Task RunPublicApiFunction(FeatureContext featureContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(featureContext);
            FunctionConfiguration functionsConfig = FunctionsBindings.GetFunctionConfiguration(featureContext);

            IConfigurationRoot config = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<IConfigurationRoot>();

            functionsConfig.CopyToEnvironmentVariables(config.AsEnumerable());

            return functionsController.StartFunctionsInstance(
                "Marain.Tenancy.Host.Functions",
                TenancyApiPort,
                "netcoreapp3.1",
                "csharp",
                functionsConfig);
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
                featureContext.RunAndStoreExceptions(functionsController.TeardownFunctions);
            }
        }
    }
}