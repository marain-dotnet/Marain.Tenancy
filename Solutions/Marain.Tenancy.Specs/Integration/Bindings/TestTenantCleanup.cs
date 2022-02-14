// <copyright file="TestTenantCleanup.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Bindings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Corvus.Testing.SpecFlow;

    using Marain.Tenancy.OpenApi;

    using Menes;

    using Microsoft.Extensions.DependencyInjection;

    using TechTalk.SpecFlow;

    [Binding]
    public class TestTenantCleanup
    {
        private static readonly HttpClient HttpClient = new();
        private readonly HashSet<(string ParentId, string TenantId)> tenantsToDelete = new();

        public  void AddTenantToDelete(string parentId, string id)
        {
            this.tenantsToDelete.Add((parentId, id));
        }

        public void AddWellKnownTenantToDelete(string parentId, string id)
        {
            this.tenantsToDelete.Add((parentId, id));
        }

        [AfterScenario("@useTenancyFunction")]
        public async Task CleanUpTestTenants(FeatureContext featureContext)
        {
            IServiceProvider serviceProvider = ContainerBindings.GetServiceProvider(featureContext);
            var errors = new List<Exception>();
            foreach ((string parentId, string id) in this.tenantsToDelete.OrderByDescending(t => t.ParentId.Length + t.TenantId.Length))
            {
                try
                {
                    if (FunctionBindings.TestHostMode != MultiHost.TestHostModes.DirectInvocation)
                    {
                        var deleteUri = new Uri(FunctionBindings.TenancyApiBaseUri, $"/{parentId}/marain/tenant/children/{id}");
                        HttpResponseMessage response = await HttpClient.DeleteAsync(deleteUri)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        // We were in direct mode, so there is no service with a delete endpoint
                        // we can hit. Instead, we need to invoke the service method.
                        TenancyService service = serviceProvider.GetRequiredService<TenancyService>();

                        await service.DeleteChildTenantAsync(
                            parentId,
                            id,
                            serviceProvider.GetRequiredService<SimpleOpenApiContext>());
                    }
                }
                catch (Exception x)
                {
                    errors.Add(x);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException(errors);
            }
        }
    }
}