// <copyright file="ClientTenantProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Corvus.Tenancy;
    using Corvus.Tenancy.Exceptions;
    using Marain.Tenancy.Client;
    using Marain.Tenancy.Mappers;
    using Microsoft.Rest;

    // Note that we do not add a using statment for Marain.Client.Models as this is the "mapping" namespace and could
    // cause collisions with the types in Marain.Tenancy.

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

            HttpOperationResponse<object, Client.Models.GetTenantHeaders> tenant = await this.TenantService.GetTenantWithHttpMessagesAsync(tenantId, eTag).ConfigureAwait(false);

            if (tenant.Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new TenantNotFoundException();
            }

            if (tenant.Response.StatusCode == HttpStatusCode.NotModified)
            {
                throw new TenantNotModifiedException();
            }

            return this.TenantMapper.MapTenant(tenant.Body);
        }
    }
}