// <copyright file="FunctionBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings
{
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
        /// <summary>
        /// Runs the public API function.
        /// </summary>
        /// <param name="featureContext">The current feature context.</param>
        /// <param name="scenarioContext">The current scenario context.</param>
        /// <returns>A task that completes when the functions have been started.</returns>
        [BeforeScenario("useTenancyFunction", Order = ContainerBeforeScenarioOrder.ServiceProviderAvailable)]
        public static Task RunPublicApiFunction(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            FunctionsController functionsController = FunctionsBindings.GetFunctionsController(scenarioContext);
            FunctionConfiguration functionsConfig = FunctionsBindings.GetFunctionConfiguration(scenarioContext);

            IConfigurationRoot config = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<IConfigurationRoot>();

            functionsConfig.CopyToEnvironmentVariables(config.AsEnumerable());

            return functionsController.StartFunctionsInstance(
                TestContext.CurrentContext.TestDirectory,
                "Marain.Tenancy.Host.Functions",
                7071,
                "netcoreapp3.1",
                "csharp",
                functionsConfig);
        }

        /// <summary>
        /// Tear down the running functions instances for the feature.
        /// </summary>
        /// <param name="scenarioContext">The current scenario context.</param>
        [AfterScenario(Order = 100)]
        public static void TeardownFunctionsAfterScenario(ScenarioContext scenarioContext)
        {
            if (scenarioContext.TryGetValue(out FunctionsController functionsController))
            {
                scenarioContext.RunAndStoreExceptions(functionsController.TeardownFunctions);
            }
        }
    }
}