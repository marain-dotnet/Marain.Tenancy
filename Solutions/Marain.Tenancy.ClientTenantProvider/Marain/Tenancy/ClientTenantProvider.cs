// <copyright file="ClientTenantProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy;

using System;
using System.Threading.Tasks;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Models;
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
    /// <param name="tenantService">The tenant service.</param>
    /// <param name="tenantMapper">The tenant mapper to use.</param>
    public ClientTenantProvider(RootTenant root, ITenancyService tenantService, ITenantMapper tenantMapper)
    {
        this.Root = root ?? throw new ArgumentNullException(nameof(root));
        this.TenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        this.TenantMapper = tenantMapper ?? throw new ArgumentNullException(nameof(tenantMapper));
    }

    /// <inheritdoc/>
    public RootTenant Root { get; }

    /// <summary>
    /// Gets the tenancy service.
    /// </summary>
    protected ITenancyService TenantService { get; }

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
            TenantResponse? tenant = await this.TenantService.GetTenantAsync(tenantId, eTag).ConfigureAwait(false);

            if (tenant == null)
            {
                throw new TenantNotFoundException();
            }

            return this.TenantMapper.MapTenant(tenant);
        }
        catch (ProblemDetails ex) when (ex.Status == 404)
        {
            throw new TenantNotFoundException();
        }
        catch (HttpValidationProblemDetails ex) when (ex.Status == 400)
        {
            throw new ArgumentException($"Invalid tenant request: {ex.Detail ?? ex.Title}");
        }
    }
}