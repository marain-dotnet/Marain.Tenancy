// <copyright file="ClientTenantProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy;

using System;
using System.Net;
using System.Threading.Tasks;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using Marain.Clients;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Resources;
using Marain.Tenancy.Mappers;

/// <summary>
/// An <see cref="ITenantProvider"/> built over a Marain tenancy instance.
/// </summary>
public class ClientTenantProvider : ITenantProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientTenantProvider"/> class.
    /// </summary>
    /// <param name="root">The Root tenant.</param>
    /// <param name="apiClient">The tenant service.</param>
    /// <param name="tenantMapper">The tenant mapper to use.</param>
    public ClientTenantProvider(RootTenant root, ITenancyClient apiClient, ITenantMapper tenantMapper)
    {
        this.Root = root ?? throw new ArgumentNullException(nameof(root));
        this.TenantApiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        this.TenantMapper = tenantMapper ?? throw new ArgumentNullException(nameof(tenantMapper));
    }

    /// <inheritdoc/>
    public RootTenant Root { get; }

    /// <summary>
    /// Gets the tenancy service.
    /// </summary>
    protected ITenancyClient TenantApiClient { get; }

    /// <summary>
    /// Gets the tenant mapper.
    /// </summary>
    protected ITenantMapper TenantMapper { get; }

    /// <inheritdoc/>
    public async Task<ITenant> GetTenantAsync(string tenantId, string? eTag = null)
    {
        // The root tenant is a special case - it lives just in memory. This is because
        // services use it to configure service-specific defaults.
        if (tenantId == this.Root.Id)
        {
            return this.Root;
        }

        try
        {
            ApiResponse<TenantResource> tenantResponse = await this.TenantApiClient.GetTenantAsync(tenantId, eTag).ConfigureAwait(false);

            tenantResponse.Headers.TryGetValue("etag", out string? etagValue);

            return this.TenantMapper.MapTenant(tenantResponse.Body, etagValue);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new TenantNotFoundException();
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ArgumentException($"Invalid tenant request: {ex.Message}");
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotModified)
        {
            throw new TenantNotModifiedException();
        }
    }
}