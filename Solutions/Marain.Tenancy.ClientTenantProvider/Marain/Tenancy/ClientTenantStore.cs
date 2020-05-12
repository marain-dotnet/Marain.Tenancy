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
    using Corvus.Extensions.Json;
    using Corvus.Tenancy;
    using Corvus.Tenancy.Exceptions;
    using Marain.Tenancy.Client;
    using Marain.Tenancy.Client.Models;
    using Marain.Tenancy.Mappers;
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    // Note that we do not add a using statment for Marain.Client.Models as this is the "mapping" namespace and could
    // cause collisions with the types in Marain.Tenancy.

    /// <summary>
    /// An <see cref="ITenantProvider"/> built over a Marain tenancy instance.
    /// </summary>
    public class ClientTenantStore : ClientTenantProvider, ITenantStore
    {
        private readonly IJsonSerializerSettingsProvider jsonSerializerSettingsProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientTenantProvider"/> class.
        /// </summary>
        /// <param name="root">The Root tenant.</param>
        /// <param name="tenantService">The tenant service.</param>
        /// <param name="tenantMapper">The tenant mapper to use.</param>
        /// <param name="jsonSerializerSettingsProvider">The JSON serializer settings provider.</param>
        public ClientTenantStore(
            RootTenant root,
            ITenancyService tenantService,
            ITenantMapper tenantMapper,
            IJsonSerializerSettingsProvider jsonSerializerSettingsProvider)
            : base(root, tenantService, tenantMapper)
        {
            this.jsonSerializerSettingsProvider = jsonSerializerSettingsProvider
                ?? throw new ArgumentNullException(nameof(jsonSerializerSettingsProvider));
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
        public async Task<TenantCollectionResult> GetChildrenAsync(string tenantId, int limit = 20, string? continuationToken = null)
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

            string? ct = this.TenantMapper.ExtractContinationTokenFrom(this.TenantService.BaseUri, (string)haldoc.SelectToken("_links.next.href"));
            return new TenantCollectionResult(tenantIds.ToList(), ct);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <para>
        /// The tenancy service uses JSON Patch to describe changes. This means that the body of
        /// the request is a JSON Array with one entry for each change to be made. For example,
        /// if you just wish to rename the tenant, your code would just need to do this:
        /// </para>
        /// <code><![CDATA[
        /// await tenantStore.UpdateTenantAsync(tenantId, name: "NewTenantName");
        /// ]]></code>
        /// <para>
        /// This method would then send an HTTP PATCH request to the tenancy service with the
        /// following content:
        /// </para>
        /// <code><![CDATA[
        ///  [{
        ///    "path": "/name",
        ///    "op": "replace",
        ///    "value": "NewTenantName"
        ///  }]
        /// ]]></code>
        /// <para>
        /// If you pass a non-null <c>propertiesToSetOrAdd</c> argument, the request will include
        /// one entry for each property being set or updated, e.g.:
        /// </para>
        /// <code><![CDATA[
        ///  [
        ///     {
        ///         "op": "add",
        ///         "path": "/properties/StorageConfiguration__corvustenancy",
        ///         "value": {
        ///            "AccountName": "mardevtenancy",
        ///            "Container": null,
        ///            "KeyVaultName": "mardevkv",
        ///            "AccountKeySecretName": "mardevtenancystore",
        ///            "DisableTenantIdPrefix": false
        ///        }
        ///     },
        ///     {
        ///         "op": "add",
        ///         "path": "/properties/Foo__bar",
        ///         "value": "Some string"
        ///     },
        ///     {
        ///         "op": "add",
        ///         "path": "/properties/Foo__spong",
        ///         "value": 42
        ///     }
        ///  ]
        /// ]]></code>
        /// <para>
        /// If the <c>propertiesToRemove</c> argument is non-null, each entry it contains will
        /// result in an entry in the patch with an <c>op</c> of <c>remove</c>.
        /// </para>
        /// <para>
        /// JSON Patch allows any combination of operations, so a single request may add, change,
        /// or remove anything.
        /// </para>
        /// <para>
        /// Note that the <c>add</c> semantics in JSON Patch are really add-or-replace, so there
        /// is no need for this client to check whether properties exist first and to set the
        /// operation type appropriately. Note however that a tenant always has a name, so we
        /// always specify <c>replace</c> when asking to change the name.
        /// </para>
        /// </remarks>
        public async Task<ITenant> UpdateTenantAsync(
            string tenantId,
            string? name,
            IEnumerable<KeyValuePair<string, object>>? propertiesToSetOrAdd = null,
            IEnumerable<string>? propertiesToRemove = null)
        {
            var patch = new List<UpdateTenantJsonPatchEntry>();

            if (name is string)
            {
                patch.Add(new UpdateTenantJsonPatchEntry("/name", "replace", name));
            }

            if (!(propertiesToSetOrAdd is null))
            {
                // When adding new values, we convert them to JTokens here so that we can use our own serializer
                // settings. Once we send things into the generated TenantService class, we lose control of
                // serialization. This is especially dangerous because the SafeJsonConvert class used internally
                // doesn't serialize read-only properties, and we tend to make extensive use of read-only properties
                // when nullable reference types are enabled.
                // I haven't found any explicit mention of this behaviour in the docs, but it is mentioned as being
                // by design in this autorest issue: https://github.com/Azure/autorest/issues/1904
                var serializer = JsonSerializer.Create(this.jsonSerializerSettingsProvider.Instance);

                foreach (KeyValuePair<string, object> kv in propertiesToSetOrAdd)
                {
                    patch.Add(
                        new UpdateTenantJsonPatchEntry(
                            "/properties/" + kv.Key,
                            "add",
                            JToken.FromObject(kv.Value, serializer)));
                }
            }

            if (!(propertiesToRemove is null))
            {
                foreach (string propertyName in propertiesToRemove)
                {
                    patch.Add(new UpdateTenantJsonPatchEntry("/properties/" + propertyName, "remove"));
                }
            }

            HttpOperationResponse<object> result = await this.TenantService.UpdateTenantWithHttpMessagesAsync(tenantId, patch).ConfigureAwait(false);

            if (result.Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new TenantNotFoundException();
            }

            if (result.Response.StatusCode == HttpStatusCode.MethodNotAllowed)
            {
                throw new NotSupportedException("This tenant cannot be updated");
            }

            result.Response.EnsureSuccessStatusCode();

            return this.TenantMapper.MapTenant(result.Body);
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
