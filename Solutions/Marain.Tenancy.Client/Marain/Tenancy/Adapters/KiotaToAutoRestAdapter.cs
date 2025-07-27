// <copyright file="KiotaToAutoRestAdapter.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Adapters;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Models;
using Marain.Tenancy.KiotaClient;
using Marain.Tenancy.KiotaClient.Models;
using Microsoft.Rest;

/// <summary>
/// Adapter that provides AutoRest-compatible interface using Kiota client underneath.
/// This enables gradual migration from AutoRest to Kiota while maintaining backward compatibility.
/// </summary>
public class KiotaToAutoRestAdapter : ITenancyServiceAdapter
{
    private readonly ITenancyKiotaService kiotaService;

    /// <summary>
    /// Initializes a new instance of the <see cref="KiotaToAutoRestAdapter"/> class.
    /// </summary>
    /// <param name="kiotaService">The Kiota-based tenancy service.</param>
    public KiotaToAutoRestAdapter(ITenancyKiotaService kiotaService)
    {
        this.kiotaService = kiotaService ?? throw new ArgumentNullException(nameof(kiotaService));
    }

    /// <inheritdoc/>
    public async Task<HttpOperationResponse<Tenant>> GetTenantWithHttpMessagesAsync(string tenantId, string? ifNoneMatch = null, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
    {
        try
        {
            TenantResponse? kiotaResponse = await this.kiotaService.GetTenantAsync(tenantId, ifNoneMatch, cancellationToken);

            if (kiotaResponse == null)
            {
                return new HttpOperationResponse<Tenant>
                {
                    Response = new HttpResponseMessage(HttpStatusCode.NotFound),
                    Body = null,
                };
            }

            Tenant autoRestTenant = this.ConvertToAutoRestTenant(kiotaResponse);

            return new HttpOperationResponse<Tenant>
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK),
                Body = autoRestTenant,
            };
        }
        catch (Exception ex)
        {
            return new HttpOperationResponse<Tenant>
            {
                Response = new HttpResponseMessage(HttpStatusCode.InternalServerError),
                Body = null,
            };
        }
    }

    /// <inheritdoc/>
    public async Task<HttpOperationResponse<Tenant>> UpdateTenantWithHttpMessagesAsync(string tenantId, IList<UpdateTenantJsonPatchEntry> patchDocument, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
    {
        try
        {
            UpdateTenantRequestJsonPatchDocument kiotaPatchDoc = this.ConvertToKiotaPatchDocument(patchDocument);
            TenantResponse? kiotaResponse = await this.kiotaService.UpdateTenantAsync(tenantId, kiotaPatchDoc, cancellationToken);

            if (kiotaResponse == null)
            {
                return new HttpOperationResponse<Tenant>
                {
                    Response = new HttpResponseMessage(HttpStatusCode.NotFound),
                    Body = null,
                };
            }

            Tenant autoRestTenant = this.ConvertToAutoRestTenant(kiotaResponse);

            return new HttpOperationResponse<Tenant>
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK),
                Body = autoRestTenant,
            };
        }
        catch (Exception ex)
        {
            return new HttpOperationResponse<Tenant>
            {
                Response = new HttpResponseMessage(HttpStatusCode.InternalServerError),
                Body = null,
            };
        }
    }

    /// <inheritdoc/>
    public async Task<HttpOperationResponse<ChildTenants>> GetChildrenWithHttpMessagesAsync(string tenantId, string? continuationToken = null, int? maxItems = null, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
    {
        try
        {
            ChildTenantsResponse? kiotaResponse = await this.kiotaService.GetChildTenantsAsync(tenantId, continuationToken, maxItems, cancellationToken);

            if (kiotaResponse == null)
            {
                return new HttpOperationResponse<ChildTenants>
                {
                    Response = new HttpResponseMessage(HttpStatusCode.NotFound),
                    Body = null,
                };
            }

            ChildTenants autoRestChildTenants = this.ConvertToAutoRestChildTenants(kiotaResponse);

            return new HttpOperationResponse<ChildTenants>
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK),
                Body = autoRestChildTenants,
            };
        }
        catch (Exception ex)
        {
            return new HttpOperationResponse<ChildTenants>
            {
                Response = new HttpResponseMessage(HttpStatusCode.InternalServerError),
                Body = null,
            };
        }
    }

    /// <inheritdoc/>
    public async Task<HttpOperationResponse> CreateChildTenantWithHttpMessagesAsync(string tenantId, string tenantName, Guid? wellKnownChildTenantGuid = null, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
    {
        try
        {
            CreateChildTenantRequest request = new()
            {
                TenantName = tenantName,
                WellKnownChildTenantGuid = wellKnownChildTenantGuid,
            };

            await this.kiotaService.CreateChildTenantAsync(tenantId, request, cancellationToken);

            return new HttpOperationResponse
            {
                Response = new HttpResponseMessage(HttpStatusCode.Created),
            };
        }
        catch (Exception ex)
        {
            return new HttpOperationResponse
            {
                Response = new HttpResponseMessage(HttpStatusCode.InternalServerError),
            };
        }
    }

    /// <inheritdoc/>
    public async Task<HttpOperationResponse> DeleteChildTenantWithHttpMessagesAsync(string tenantId, string childTenantId, Dictionary<string, List<string>>? customHeaders = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await this.kiotaService.DeleteChildTenantAsync(tenantId, childTenantId, cancellationToken);

            return new HttpOperationResponse
            {
                Response = new HttpResponseMessage(HttpStatusCode.OK),
            };
        }
        catch (Exception ex)
        {
            return new HttpOperationResponse
            {
                Response = new HttpResponseMessage(HttpStatusCode.InternalServerError),
            };
        }
    }

    private Tenant ConvertToAutoRestTenant(TenantResponse kiotaResponse)
    {
        return new Tenant(
            id: kiotaResponse.Id ?? string.Empty,
            name: kiotaResponse.Name ?? string.Empty,
            contentType: kiotaResponse.ContentType ?? string.Empty,
            _links: this.ConvertLinks(kiotaResponse.Links),
            properties: this.ConvertProperties(kiotaResponse.Properties));
    }

    private ChildTenants ConvertToAutoRestChildTenants(ChildTenantsResponse kiotaResponse)
    {
        return new ChildTenants(
            _links: this.ConvertLinks(kiotaResponse.Links),
            _embedded: this.ConvertEmbedded(kiotaResponse.Embedded));
    }

    private UpdateTenantRequestJsonPatchDocument ConvertToKiotaPatchDocument(IList<UpdateTenantJsonPatchEntry> autoRestPatch)
    {
        List<UpdateTenantRequestOperation> operations = autoRestPatch.Select(entry => new UpdateTenantRequestOperation
        {
            Op = entry.Op,
            Path = entry.Path,
            Value = entry.Value,
        }).ToList();

        return new UpdateTenantRequestJsonPatchDocument
        {
            // Note: The exact property name may vary based on Kiota generation
            // This would need to be adjusted based on the actual generated model
        };
    }

    private IDictionary<string, object>? ConvertLinks(TenantResponse__links? kiotaLinks)
    {
        if (kiotaLinks == null)
        {
            return null;
        }

        // Convert Kiota links to AutoRest format
        // This would need implementation based on the actual structure
        return new Dictionary<string, object>();
    }

    private IDictionary<string, object>? ConvertProperties(TenantResponse_properties? kiotaProperties)
    {
        if (kiotaProperties == null)
        {
            return null;
        }

        // Convert Kiota properties to AutoRest format
        // This would need implementation based on the actual structure
        return new Dictionary<string, object>();
    }

    private IDictionary<string, object>? ConvertEmbedded(ChildTenantsEmbedded? kiotaEmbedded)
    {
        if (kiotaEmbedded == null)
        {
            return null;
        }

        // Convert Kiota embedded to AutoRest format
        // This would need implementation based on the actual structure
        return new Dictionary<string, object>();
    }
}