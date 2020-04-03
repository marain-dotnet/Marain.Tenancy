// <copyright file="FunctionBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings
{
    using System.Threading.Tasks;
    using Corvus.SpecFlow.Extensions;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
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
        /// <returns>A task that completes when the functions have been started.</returns>
        [BeforeScenario("useTenancyFunction", Order = ContainerBeforeScenarioOrder.ServiceProviderAvailable)]
        public static async Task RunPublicApiFunction(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            var functionsController = new FunctionsController();
            scenarioContext.Set(functionsController);

            IConfigurationRoot config = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<IConfigurationRoot>();
            scenarioContext.CopyToFunctionConfigurationEnvironmentVariables(config);

            await functionsController.StartFunctionsInstance(featureContext, scenarioContext, "Marain.Tenancy.Host.Functions", 7071, "netcoreapp3.1");
        }

        /// <summary>
        /// Tear down the running functions instances for the feature.
        /// </summary>
        /// <param name="featureContext">The current feature context.</param>
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