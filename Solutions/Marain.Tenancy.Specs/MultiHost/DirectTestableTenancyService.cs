using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Marain.Tenancy.OpenApi;

namespace Marain.Tenancy.Specs.MultiHost
{
    internal class DirectTestableTenancyService : ITestableTenancyService
    {
        private TenancyService tenancyService;

        public DirectTestableTenancyService(TenancyService tenancyService)
        {
            this.tenancyService = tenancyService;
        }

        public Task<TenancyResponse> CreateTenantAsync(string parentId, string name)
        {
            throw new NotImplementedException();
        }

        public Task<TenancyResponse> GetSwaggerAsync()
        {
            throw new NotImplementedException();
        }

        public Task<TenancyResponse> GetTenantAsync(string tenantId, string? etag)
        {
            throw new NotImplementedException();
        }

        public Task<TenancyResponse> GetTenantByLocationAsync(string location)
        {
            throw new NotImplementedException();
        }
    }
}
