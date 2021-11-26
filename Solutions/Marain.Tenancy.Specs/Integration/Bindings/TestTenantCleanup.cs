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

    using TechTalk.SpecFlow;

    [Binding]
    public class TestTenantCleanup
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly HashSet<(string ParentId, string TenantId)> tenantsToDelete = new();

        public  void AddTenantToDelete(string parentId, string id)
        {
            this.tenantsToDelete.Add((parentId, id));
        }

        [AfterScenario("@useTenancyFunction")]
        public async Task CleanUpTestTenants()
        {
            var errors = new List<Exception>();
            foreach ((string parentId, string id) in this.tenantsToDelete.OrderByDescending(t => t.ParentId.Length + t.TenantId.Length))
            {
                try
                {
                    var deleteUri = new Uri(FunctionBindings.TenancyApiBaseUri, $"/{parentId}/marain/tenant/children/{id}");
                    HttpResponseMessage response = await HttpClient.DeleteAsync(deleteUri)
                        .ConfigureAwait(false);
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
