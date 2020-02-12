// <copyright file="FunctionBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading.Tasks;
    using Corvus.SpecFlow.Extensions;
    using Corvus.SpecFlow.Extensions.Internal;
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
        [BeforeFeature("useTenancyFunction", Order = ContainerBeforeFeatureOrder.ServiceProviderAvailable)]
        public static async Task RunPublicApiFunction(FeatureContext featureContext)
        {
            var functionsController = new FunctionsController();
            featureContext.Set(functionsController);

            IConfigurationRoot config = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<IConfigurationRoot>();
            featureContext.CopyToFunctionConfigurationEnvironmentVariables(config);

            await functionsController.StartFunctionsInstance(featureContext, null, "Marain.Tenancy.Host.Functions", 7071, "netcoreapp3.1");
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

        [AfterScenario]
        public static void ShowErrorsSoFarIfTestFailed(
            FeatureContext featureContext,
            ScenarioContext scenarioContext)
        {
            if (featureContext.TryGetValue(out FunctionsController functionsController))
            {
                FieldInfo fi = typeof(FunctionsController).GetField("output", BindingFlags.Instance | BindingFlags.NonPublic);
                var output = (IDictionary<Process, FunctionOutputBufferHandler>)fi.GetValue(functionsController);

                foreach (Process p in output.Keys)
                {
                    string name =
                        $"{p.StartInfo.FileName} {p.StartInfo.Arguments}, working directory {p.StartInfo.WorkingDirectory}";

                    Console.WriteLine($"\nStdOut for process {name}:");
                    Console.WriteLine(output[p].StandardOutputText);
                    Console.WriteLine();

                    string stdErr = output[p].StandardErrorText;

                    if (!string.IsNullOrEmpty(stdErr))
                    {
                        Console.WriteLine($"\nStdErr for process {name}:");
                        Console.WriteLine(stdErr);
                        Console.WriteLine();
                    }
                }
            }
        }
    }
}