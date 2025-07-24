namespace Marain.Tenancy.Client;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Marain.Tenancy.Client.Internal;
using Marain.Tenancy.Client.Mappers;
using Marain.Tenancy.Client.Models;
using Microsoft.Kiota.Abstractions;

public class TenancyService : ITenancyService
{
    private readonly MarainTenancyClient client;

    public TenancyService(MarainTenancyClient client)
    {
        this.client = client;
    }

    public Task CreateChildTenantAsync(string tenantId, string tenantName, Guid? wellKnownChildTenantGuid = default, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteChildTenantAsync(string tenantId, string childTenantId, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<GetChildrenResult> GetChildrenAsync(string tenantId, string? continuationToken = default, int? maxItems = default, CancellationToken? cancellationToken = default)
    {
        Internal.Models.Resource? result = await this.client[tenantId].Marain.Tenant.Children.GetAsync(null, cancellationToken ?? CancellationToken.None);

        // TODO: Not found?
        if (result is null)
        {
			throw new Exception();
		}

        return GetChildrenResultMapper.FromInternalModel(result);
    }

    public async Task<Tenant> GetTenantAsync(string tenantId, string? etag = default, CancellationToken? cancellationToken = default)
    {
        // Add etag if sdupplied
        Internal.Models.Tenant? result = await this.client[tenantId].Marain.Tenant.GetAsync((requestConfiguration) => { }, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
        if (result is null)
        {
            // Not found?
            throw new Exception();
        }

        return TenantMapper.FromInternalModel(result);
    }

    public Task UpdateTenantAsync(string tenantId, IList<UpdateTenantJsonPatchEntry> body, CancellationToken? cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
