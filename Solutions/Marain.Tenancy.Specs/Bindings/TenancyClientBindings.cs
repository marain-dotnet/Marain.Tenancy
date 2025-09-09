// <copyright file="TenancyClientBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Bindings;

using System.Linq;
using Corvus.Identity.ClientAuthentication;
using Corvus.Testing.ReqnRoll;
using Marain.Tenancy.Specs.Helpers;
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
                bool enableResponseCaching = !featureContext.FeatureInfo.Tags.Contains("disableTenantCaching");

                TenancyApiClientConfiguration config = new()
                {
                    BaseUri = ApiWebApplicationFactory.Current.Server.BaseAddress.ToString(),
                    ResourceIdForMsiAuthentication = "placeholder", // To ensure auth is configured
                };

                serviceCollection.AddSingleton<IServiceIdentityAccessTokenSource, TestAccessTokenSource>();

                serviceCollection.AddTenancyClient(
                    _ => config,
                    enableResponseCaching,
                    ApiWebApplicationFactory.Current.Server.CreateHandler());
            });
    }
}