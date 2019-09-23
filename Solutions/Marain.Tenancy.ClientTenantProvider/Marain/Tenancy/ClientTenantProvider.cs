﻿// <copyright file="ClientTenantProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Corvus.Tenancy;
    using Corvus.Tenancy.Exceptions;
    using Marain.Tenancy.Client;
    using Marain.Tenancy.Mappers;
    using Microsoft.Rest;
    using Newtonsoft.Json.Linq;

    // Note that we do not add a using statment for Marain.Client.Models as this is the "mapping" namespace and could
    // cause collisions with the types in Marain.Tenancy.

    /// <summary>
    /// An <see cref="ITenantProvider"/> built over a Marain tenancy instance.
    /// </summary>
    public class ClientTenantProvider : ITenantProvider
    {
        private readonly ITenancyService tenantService;
        private readonly ITenantMapper tenantMapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientTenantProvider"/> class.
        /// </summary>
        /// <param name="root">The Root tenant.</param>
        /// <param name="tenantService">The tenant service.</param>
        /// <param name="tenantMapper">The tenant mapper to use.</param>
        public ClientTenantProvider(RootTenant root, ITenancyService tenantService, ITenantMapper tenantMapper)
        {
            this.Root = root ?? throw new ArgumentNullException(nameof(root));
            this.tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
            this.tenantMapper = tenantMapper ?? throw new ArgumentNullException(nameof(tenantMapper));
        }

        /// <inheritdoc/>
        public ITenant Root { get; }

        /// <inheritdoc/>
        public async Task<ITenant> CreateChildTenantAsync(string parentTenantId)
        {
            HttpOperationHeaderResponse<Client.Models.CreateChildTenantHeaders> result = await this.tenantService.CreateChildTenantWithHttpMessagesAsync(parentTenantId).ConfigureAwait(false);
            if (result.Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new TenantNotFoundException();
            }

            if (result.Response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new TenantConflictException();
            }

            if (result.Response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new InvalidOperationException();
            }

            if (!result.Response.IsSuccessStatusCode)
            {
                throw new Exception(result.Response.ReasonPhrase);
            }

            object tenant = await this.tenantService.GetTenantAsync(this.tenantMapper.ExtractTenantIdFrom(this.tenantService.BaseUri, result.Headers.Location)).ConfigureAwait(false);
            return this.tenantMapper.MapTenant(tenant);
        }

        /// <inheritdoc/>
        public async Task DeleteTenantAsync(string tenantId)
        {
            HttpOperationResponse result = await this.tenantService.DeleteChildTenantWithHttpMessagesAsync(tenantId.GetParentId(), tenantId).ConfigureAwait(false);
            if (result.Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new TenantNotFoundException();
            }

            if (result.Response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new InvalidOperationException();
            }

            if (!result.Response.IsSuccessStatusCode)
            {
                throw new Exception(result.Response.ReasonPhrase);
            }
        }

        /// <inheritdoc/>
        public async Task<TenantCollectionResult> GetChildrenAsync(string tenantId, int limit = 20, string continuationToken = null)
        {
            HttpOperationResponse<object> result = await this.tenantService.GetChildrenWithHttpMessagesAsync(tenantId, continuationToken, limit).ConfigureAwait(false);

            if (result.Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new TenantNotFoundException();
            }

            if (result.Response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new InvalidOperationException();
            }

            if (!result.Response.IsSuccessStatusCode)
            {
                throw new Exception(result.Response.ReasonPhrase);
            }

            var haldoc = (JObject)result.Body;
            IEnumerable<string> tenantIds = haldoc["_links"]["getTenant"].Cast<JObject>().Select(link => this.tenantMapper.ExtractTenantIdFrom(this.tenantService.BaseUri, (string)link["href"]));
            string ct = this.tenantMapper.ExtractContinationTokenFrom(this.tenantService.BaseUri, (string)haldoc.SelectToken("_links.next.href"));
            return new TenantCollectionResult(tenantIds.ToList(), ct);
        }

        /// <inheritdoc/>
        public async Task<ITenant> GetTenantAsync(string tenantId, string eTag = null)
        {
            HttpOperationResponse<object, Client.Models.GetTenantHeaders> tenant = await this.tenantService.GetTenantWithHttpMessagesAsync(tenantId, eTag).ConfigureAwait(false);

            if (tenant.Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new TenantNotFoundException();
            }

            if (tenant.Response.StatusCode == HttpStatusCode.NotModified)
            {
                throw new TenantNotModifiedException();
            }

            return this.tenantMapper.MapTenant(tenant.Body);
        }

        /// <inheritdoc/>
        public async Task<ITenant> UpdateTenantAsync(ITenant tenant)
        {
            HttpOperationResponse<object> result = await this.tenantService.UpdateTenantWithHttpMessagesAsync(tenant.Id, this.tenantMapper.MapTenant(tenant)).ConfigureAwait(false);
            if (result.Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new TenantNotFoundException();
            }

            return this.tenantMapper.MapTenant(result.Body);
        }
    }
}