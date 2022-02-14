// <copyright file="ITestableTenancyService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.MultiHost
{
    using System.Threading.Tasks;

    public interface ITestableTenancyService
    {
        Task<TenancyResponse> GetTenantAsync(string tenantId, string? etag = null);

        Task<TenancyResponse> CreateTenantAsync(string parentId, string name);

        Task<TenancyResponse> GetSwaggerAsync();

        Task<TenancyResponse> GetTenantByLocationAsync(string location);
    }
}