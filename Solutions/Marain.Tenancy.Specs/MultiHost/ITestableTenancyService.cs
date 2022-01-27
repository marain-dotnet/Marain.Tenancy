using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marain.Tenancy.Specs.MultiHost
{
    public interface ITestableTenancyService
    {
        Task<TenancyResponse> GetTenantAsync(string tenantId, string? etag = null);
        Task<TenancyResponse> CreateTenantAsync(string parentId, string name);
        Task<TenancyResponse> GetSwaggerAsync();
        Task<TenancyResponse> GetTenantByLocationAsync(string location);
    }
}
