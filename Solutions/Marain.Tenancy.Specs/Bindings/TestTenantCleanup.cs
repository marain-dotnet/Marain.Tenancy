// <copyright file="TestTenantCleanup.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Bindings;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Marain.Tenancy.Specs.Helpers;
using Reqnroll;

[Binding]
public static class TestTenantCleanup
{
    private static readonly HashSet<(string ParentId, string TenantId)> TenantsToDelete = [];

    public static void AddTenantToDelete(string? parentId, string? id)
    {
        if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(id))
        {
            TenantsToDelete.Add((parentId, id));
        }
    }

    public static void RemoveTenantToDelete(string? parentId, string? id)
    {
        if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(id))
        {
            TenantsToDelete.RemoveWhere(x => x == (parentId, id));
        }
    }

    [AfterScenario("@useTenancyApi")]
    public static Task CleanUpTestTenants(FeatureContext featureContext)
    {
        return Task.CompletedTask;
        ////var errors = new List<Exception>();
        ////foreach ((string parentId, string id) in TenantsToDelete.OrderByDescending(t => t.ParentId.Length + t.TenantId.Length))
        ////{
        ////    try
        ////    {
        ////        HttpResponseMessage response = await ApiWebApplicationFactory.Current.Client.DeleteAsync(
        ////            $"/{parentId}/marain/tenant/children/{id}").ConfigureAwait(false);
        ////        response.EnsureSuccessStatusCode();
        ////    }
        ////    catch (Exception x)
        ////    {
        ////        errors.Add(x);
        ////    }
        ////}

        ////TenantsToDelete.Clear();

        ////if (errors.Count > 0)
        ////{
        ////    throw new AggregateException(errors);
        ////}
    }
}