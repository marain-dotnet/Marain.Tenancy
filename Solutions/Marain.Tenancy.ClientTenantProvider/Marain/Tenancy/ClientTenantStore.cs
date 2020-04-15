// <copyright file="ClientTenantStore.cs" company="Endjin Limited">
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
    public class ClientTenantStore : ClientTenantProvider, ITenantStore
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientTenantProvider"/> class.
        /// </summary>
        /// <param name="root">The Root tenant.</param>
        /// <param name="tenantService">The tenant service.</param>
        /// <param name="tenantMapper">The tenant mapper to use.</param>
        public ClientTenantStore(RootTenant root, ITenancyService tenantService, ITenantMapper tenantMapper)
            : base(root, tenantService, tenantMapper)
        {
        }

        /// <inheritdoc/>
        public Task<ITenant> CreateChildTenantAsync(string parentTenantId, string name)
        {
            return this.CreateChildTenantAsync(parentTenantId, name, null);
        }

        /// <inheritdoc/>
        public Task<ITenant> CreateWellKnownChildTenantAsync(string parentTenantId, Guid wellKnownChildTenantGuid, string name)
        {
            return this.CreateChildTenantAsync(parentTenantId, name, wellKnownChildTenantGuid);
        }

        /// <inheritdoc/>
        public async Task DeleteTenantAsync(string tenantId)
        {
            HttpOperationResponse result = await this.TenantService.DeleteChildTenantWithHttpMessagesAsync(tenantId.GetParentId(), tenantId).ConfigureAwait(false);
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
            HttpOperationResponse<object> result = await this.TenantService.GetChildrenWithHttpMessagesAsync(tenantId, continuationToken, limit).ConfigureAwait(false);

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

            JToken getTenantLinks = haldoc["_links"]["getTenant"];

            IEnumerable<string> tenantIds;

            if (getTenantLinks == null)
            {
                tenantIds = new string[0];
            }
            else if (getTenantLinks is JArray)
            {
                tenantIds = getTenantLinks.Cast<JObject>().Select(link => this.TenantMapper.ExtractTenantIdFrom(this.TenantService.BaseUri, (string)link["href"]));
            }
            else
            {
                tenantIds = new string[]
                {
                    this.TenantMapper.ExtractTenantIdFrom(this.TenantService.BaseUri, (string)getTenantLinks["href"]),
                };
            }

            string ct = this.TenantMapper.ExtractContinationTokenFrom(this.TenantService.BaseUri, (string)haldoc.SelectToken("_links.next.href"));
            return new TenantCollectionResult(tenantIds.ToList(), ct);
        }

        /// <inheritdoc/>
        public Task<ITenant> UpdateTenantAsync(string tenantId, IEnumerable<KeyValuePair<string, object>> propertiesToSetOrAdd = null, IEnumerable<string> propertiesToRemove = null)
        {
            throw new NotImplementedException();
            ////HttpOperationResponse<object> result = await this.tenantService.UpdateTenantWithHttpMessagesAsync(tenant.Id, this.tenantMapper.MapTenant(tenant)).ConfigureAwait(false);
            ////if (result.Response.StatusCode == HttpStatusCode.NotFound)
            ////{
            ////    throw new TenantNotFoundException();
            ////}

            ////if (result.Response.StatusCode == HttpStatusCode.MethodNotAllowed)
            ////{
            ////    throw new NotSupportedException("This tenant cannot be updated");
            ////}

            ////return this.tenantMapper.MapTenant(result.Body);
        }

        private async Task<ITenant> CreateChildTenantAsync(string parentTenantId, string name, Guid? wellKnownChildTenantGuid)
        {
            HttpOperationHeaderResponse<Client.Models.CreateChildTenantHeaders> result =
                await this.TenantService.CreateChildTenantWithHttpMessagesAsync(parentTenantId, name, wellKnownChildTenantGuid).ConfigureAwait(false);

            if (result.Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new TenantNotFoundException();
            }

            // TODO: How do we determine a duplicate Id?
            if (result.Response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new TenantConflictException();
            }

            if (result.Response.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new ArgumentException();
            }

            if (!result.Response.IsSuccessStatusCode)
            {
                throw new Exception(result.Response.ReasonPhrase);
            }

            object tenant = await this.TenantService.GetTenantAsync(
                this.TenantMapper.ExtractTenantIdFrom(this.TenantService.BaseUri, result.Headers.Location)).ConfigureAwait(false);

            return this.TenantMapper.MapTenant(tenant);
        }
    }
}
