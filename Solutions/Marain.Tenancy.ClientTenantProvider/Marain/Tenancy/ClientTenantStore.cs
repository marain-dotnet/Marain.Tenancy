// <copyright file="ClientTenantStore.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Corvus.Json;
using Corvus.Json.Serialization;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using Marain.Clients;
using Marain.Clients.Hal;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Resources;
using Marain.Tenancy.Mappers;

/// <summary>
/// An <see cref="ITenantProvider"/> built over a Marain tenancy instance.
/// </summary>
public class ClientTenantStore(
    RootTenant root,
    ITenancyClient tenancyApiClient,
    ITenantMapper tenantMapper,
    IPropertyBagFactory propertyBagFactory) : ClientTenantProvider(root, tenancyApiClient, tenantMapper), ITenantStore
{
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
        try
        {
            // Extract parent tenant ID from the full tenant ID path
            string parentTenantId = tenantId.GetParentId()
                ?? throw new InvalidOperationException("Unable to extract parent tenant Id from supplied tenant Id");

            await this.TenantApiClient.DeleteChildTenantAsync(parentTenantId, tenantId).ConfigureAwait(false);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new TenantNotFoundException(ex.Message, ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException($"Invalid delete tenant request: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<TenantCollectionResult> GetChildrenAsync(string tenantId, int limit = 20, string? continuationToken = null)
    {
        try
        {
            ApiResponse<ChildTenantsResource> response = await this.TenantApiClient.GetChildrenAsync(tenantId, continuationToken, limit).ConfigureAwait(false);

            // Extract tenant IDs from the linked tenants
            List<WebLink> childLinkResponses = response.Body.Links?.GetTenant ?? [];
            IEnumerable<string> childTenantIds = childLinkResponses
                .Select(x => x.Href)
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => this.TenantMapper.ExtractTenantIdFromUrlPath(x!));

            // Use the continuation token from the response for pagination
            string? nextContinuationToken = response.Body.ContinuationToken;

            return new TenantCollectionResult(childTenantIds, nextContinuationToken);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new TenantNotFoundException(ex.Message, ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException($"Invalid get children request: {ex.Message}", ex);
        }
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
        IPropertyBag? propertiesToAddOrUpdate = propertiesToSetOrAdd is null
            ? null
            : propertyBagFactory.Create(propertiesToSetOrAdd);

        try
        {
            ApiResponse<TenantResource> response = await this.TenantApiClient.UpdateTenantAsync(
                tenantId,
                name,
                propertiesToSetOrAdd,
                propertiesToRemove).ConfigureAwait(false);

            return this.TenantMapper.MapTenant(response.Body);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new TenantNotFoundException(ex.Message, ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.MethodNotAllowed)
        {
            throw new NotSupportedException("This tenant cannot be updated", ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ArgumentException($"Invalid update tenant request: {ex.Message}", ex);
        }
    }

    private async Task<ITenant> CreateChildTenantAsync(string parentTenantId, string name, Guid? wellKnownChildTenantGuid)
    {
        try
        {
            ApiResponse<TenantResource> response = await this.TenantApiClient.CreateChildTenantAsync(
                parentTenantId,
                name,
                wellKnownChildTenantGuid?.ToString()).ConfigureAwait(false);

            response.Headers.TryGetValue("etag", out string? etag);

            return this.TenantMapper.MapTenant(response.Body, etag);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new TenantNotFoundException(ex.Message, ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            throw new TenantConflictException(ex.Message, ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new ArgumentException($"Invalid create child tenant request: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create child tenant", ex);
        }
    }
}