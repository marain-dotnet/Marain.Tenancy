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

        public Task<TenancyResponse> GetTenantAsync(string tenantId)
        {
            throw new NotImplementedException();
        }
    }
}
