// <copyright file="FunctionBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using BoDi;

    using Corvus.Extensions.Json;
    using Corvus.Testing.AzureFunctions;
    using Corvus.Testing.AzureFunctions.SpecFlow;
    using Corvus.Testing.SpecFlow;

    using Marain.Tenancy.OpenApi;
    using Marain.Tenancy.Specs.MultiHost;
    using Menes;
    using Menes.Hal;
    using Menes.Internal;
    using Menes.Testing.AspNetCoreSelfHosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using NUnit.Framework.Internal;

    using TechTalk.SpecFlow;

    /// <summary>
    /// Provides function initialisation for tests that require endpoints to be available.
    /// </summary>
    [Binding]
    public static class FunctionBindings
    {
        private static readonly string TenancyApiBaseUriText = $"http://localhost:{TenancyApiPort}";

        public const int TenancyApiPort = 7071;

        public static readonly Uri TenancyApiBaseUri = new (TenancyApiBaseUriText);

        public static TestHostModes TestHostMode => TestExecutionContext.CurrentContext.TestObject switch
        {
            IMultiModeTest<TestHostModes> multiModeTest => multiModeTest.TestType,
            _ => TestHostModes.UseFunctionHost,
        };

        /// <summary>
        /// Runs the public API function.
        /// </summary>
        /// <param name="featureContext">The current feature context.</param>
        /// <returns>A task that completes when the functions have been started.</returns>
        [BeforeFeature("useTenancyFunction", Order = ContainerBeforeFeatureOrder.ServiceProviderAvailable)]
        public static async Task RunPublicApiFunction(
            FeatureContext featureContext,
            IObjectContainer specFlowDiContainer)
        {
            IConfigurationRoot config = ContainerBindings.GetServiceProvider(featureContext).GetRequiredService<IConfigurationRoot>();
            IServiceProvider serviceProvider = ContainerBindings.GetServiceProvider(featureContext);

            switch (TestHostMode)
            {
                case TestHostModes.InProcessEmulateFunctionWithActionResult:
                    var hostManager = new OpenApiWebHostManager();
                    featureContext.Set(hostManager);
                    await hostManager.StartInProcessFunctionsHostAsync<FunctionsStartupWrapper>(
                        TenancyApiBaseUriText,
                        config);
                    break;

                case TestHostModes.UseFunctionHost:
                    FunctionsController functionsController = FunctionsBindings.GetFunctionsController(featureContext);
                    FunctionConfiguration functionsConfig = FunctionsBindings.GetFunctionConfiguration(featureContext);

                    functionsConfig.CopyToEnvironmentVariables(config.AsEnumerable());
                    functionsConfig.EnvironmentVariables.Add("TenantCacheConfiguration__GetTenantResponseCacheControlHeaderValue", "max-age=300");

                    await functionsController.StartFunctionsInstance(
                        "Marain.Tenancy.Host.Functions",
                        TenancyApiPort,
                        "net6.0",
                        "csharp",
                        functionsConfig);
                    break;

                case TestHostModes.DirectInvocation:
                    // Doing this for the side effects only - it causes the OpenApi document
                    // to be registered in Menes.
                    serviceProvider.GetRequiredService<IOpenApiHost<HttpRequest, IActionResult>>();
                    break;
            }


            ITestableTenancyService serviceWrapper = TestHostMode == TestHostModes.DirectInvocation
                ? new DirectTestableTenancyService(
                    serviceProvider.GetRequiredService<TenancyService>(),
                    serviceProvider.GetRequiredService<SimpleOpenApiContext>())
                : new ClientTestableTenancyService(
                    TenancyApiBaseUriText,
                    serviceProvider.GetRequiredService<IJsonSerializerSettingsProvider>().Instance);

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
                featureContext.RunAndStoreExceptions(functionsController.TeardownFunctions);
            }
        }
    }
}