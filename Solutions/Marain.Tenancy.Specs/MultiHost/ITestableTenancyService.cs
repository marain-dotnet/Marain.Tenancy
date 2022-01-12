using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marain.Tenancy.Specs.MultiHost
{
    public interface ITestableTenancyService
    {
        Task<TenancyResponse> GetTenantAsync(string tenantId);
    }
}
