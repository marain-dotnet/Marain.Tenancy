// <copyright file="TestTenantCleanup.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Bindings;

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Requests;
using Reqnroll;

[Binding]
public class TestTenantCleanup
{
    private readonly HashSet<(string ParentId, string TenantId)> tenantsToDelete = new();

    public void AddTenantToDelete(string? parentId, string? id)
    {
        if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(id))
        {
            this.tenantsToDelete.Add((parentId, id));
        }
    }

    public void AddWellKnownTenantToDelete(string parentId, string id)
    {
        this.tenantsToDelete.Add((parentId, id));
    }

    [AfterScenario("@useTenancyApi")]
    public async Task CleanUpTestTenants(FeatureContext featureContext)
    {
        await Task.CompletedTask;
        ////IServiceProvider serviceProvider = ContainerBindings.GetServiceProvider(featureContext);
        ////var errors = new List<Exception>();
        ////foreach ((string parentId, string id) in this.tenantsToDelete.OrderByDescending(t => t.ParentId.Length + t.TenantId.Length))
        ////{
        ////    try
        ////    {
        ////        if (FunctionBindings.TestHostMode == MultiHost.TestHostModes.TenancyClient)
        ////        {
        ////            var deleteUri = new Uri(FunctionBindings.TenancyApiBaseUri, $"/{parentId}/marain/tenant/children/{id}");
        ////            HttpResponseMessage response = await HttpClient.DeleteAsync(deleteUri)
        ////                .ConfigureAwait(false);
        ////        }
        ////        else
        ////        {
        ////            // We were in MinimalApi mode, so use the testable service to delete
        ////            ITestableTenancyService service = serviceProvider.GetRequiredService<ITestableTenancyService>();

        ////            // For MinimalApi mode, we use HTTP client as well since we're testing the HTTP interface
        ////            var deleteUri = new Uri(FunctionBindings.TenancyApiBaseUri, $"/{parentId}/marain/tenant/children/{id}");
        ////            HttpResponseMessage response = await HttpClient.DeleteAsync(deleteUri)
        ////                .ConfigureAwait(false);
        ////        }
        ////    }
        ////    catch (Exception x)
        ////    {
        ////        errors.Add(x);
        ////    }
        ////}

        ////if (errors.Count > 0)
        ////{
        ////    throw new AggregateException(errors);
        ////}
    }
}