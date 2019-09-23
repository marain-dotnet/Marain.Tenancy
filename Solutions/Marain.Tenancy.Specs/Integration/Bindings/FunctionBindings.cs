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
        [BeforeFeature("useTenancyFunction", Order = 100)]
        public static Task RunPublicApiFunction(FeatureContext featureContext)
        {
            var functionsController = new FunctionsController();
            featureContext.Set(functionsController);

            IConfigurationRoot config = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<IConfigurationRoot>();
            featureContext.CopyToFunctionConfigurationEnvironmentVariables(config);

            return functionsController.StartFunctionsInstance(featureContext, null, "Marain.Tenancy.Host.Functions", 7071);
        }

        /// <summary>
        /// Tear down the running functions instances for the feature.
        /// </summary>
        /// <param name="featureContext">The current feature context.</param>
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
