// <copyright file="ClientTenantStore.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Corvus.Json.Serialization;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Helpers;
using Marain.Tenancy.Client.Models;
using Marain.Tenancy.Mappers;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

/// <summary>
/// An <see cref="ITenantProvider"/> built over a Marain tenancy instance.
/// </summary>
public class ClientTenantStore : ClientTenantProvider, ITenantStore
{
    private readonly IJsonSerializerOptionsProvider serializerOptionsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientTenantProvider"/> class.
    /// </summary>
    /// <param name="root">The Root tenant.</param>
    /// <param name="tenancyApiClient">The tenant service.</param>
    /// <param name="tenantMapper">The tenant mapper to use.</param>
    /// <param name="serializerOptionsProvider">The current <see cref="IJsonSerializerOptionsProvider"/>.</param>
    public ClientTenantStore(
        RootTenant root,
        TenancyApiClient tenancyApiClient,
        ITenantMapper tenantMapper,
        IJsonSerializerOptionsProvider serializerOptionsProvider)
        : base(root, tenancyApiClient, tenantMapper)
    {
        this.serializerOptionsProvider = serializerOptionsProvider;
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
        try
        {
            // Extract parent tenant ID from the full tenant ID path
            string? parentTenantId = tenantId.GetParentId();

            await this.TenantApiClient[parentTenantId].Marain.Tenant.Children[tenantId].DeleteAsync().ConfigureAwait(false);
        }
        catch (ProblemDetails ex) when (ex.Status == 404)
        {
            throw new TenantNotFoundException();
        }
        catch (HttpValidationProblemDetails ex) when (ex.Status == 400)
        {
            throw new InvalidOperationException($"Invalid delete tenant request: {ex.Detail ?? ex.Title}");
        }
    }

    /// <inheritdoc/>
    public async Task<TenantCollectionResult> GetChildrenAsync(string tenantId, int limit = 20, string? continuationToken = null)
    {
        try
        {
            ChildTenantsResponse? result = await this.TenantApiClient[tenantId].Marain.Tenant.Children.GetAsync(config =>
            {
                config.QueryParameters.ContinuationToken = continuationToken;
                config.QueryParameters.MaxItems = limit;
            }).ConfigureAwait(false);

            if (result is null)
            {
                throw new TenantNotFoundException();
            }

            // Extract tenant IDs from the linked tenants
            List<LinkResponse> childLinkResponses = result?.Links?.GetTenant ?? [];
            IEnumerable<string> childTenantIds = childLinkResponses
                .Select(x => x.Href)
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => this.TenantMapper.ExtractTenantIdFromAbsoluteUrl(x!));

            // Use the continuation token from the response for pagination
            string? nextContinuationToken = result!.ContinuationToken;

            return new TenantCollectionResult(childTenantIds, nextContinuationToken);
        }
        catch (ProblemDetails ex) when (ex.Status == 404)
        {
            throw new TenantNotFoundException();
        }
        catch (HttpValidationProblemDetails ex) when (ex.Status == 400)
        {
            throw new InvalidOperationException($"Invalid get children request: {ex.Detail ?? ex.Title}");
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
        // Create a list of patch operations
        var operations = new List<UpdateTenantJsonPatchEntry>();

        if (name is not null)
        {
            operations.Add(
                UpdateTenantJsonPatchEntryFactory.Create(
                    UpdateTenantJsonPatchEntryOperation.Replace,
                    "/name",
                    name!));
        }

        if (propertiesToSetOrAdd is not null)
        {
            foreach (KeyValuePair<string, object> kv in propertiesToSetOrAdd)
            {
                operations.Add(
                    UpdateTenantJsonPatchEntryFactory.Create(
                        UpdateTenantJsonPatchEntryOperation.Add,
                        "/properties/" + kv.Key,
                        kv.Value,
                        this.serializerOptionsProvider.Instance));
            }
        }

        if (propertiesToRemove is not null)
        {
            foreach (string propertyName in propertiesToRemove)
            {
                operations.Add(
                    UpdateTenantJsonPatchEntryFactory.CreateDeleteEntry("/properties/" + propertyName));
            }
        }

        try
        {
            TenantResponse? result = await this.TenantApiClient[tenantId].Marain.Tenant.PatchAsync(operations).ConfigureAwait(false);

            if (result == null)
            {
                throw new TenantNotFoundException();
            }

            return this.TenantMapper.MapTenant(result);
        }
        catch (ProblemDetails ex) when (ex.Status == 404)
        {
            throw new TenantNotFoundException();
        }
        catch (ProblemDetails ex) when (ex.Status == 405)
        {
            throw new NotSupportedException("This tenant cannot be updated");
        }
        catch (HttpValidationProblemDetails ex) when (ex.Status == 400)
        {
            throw new ArgumentException($"Invalid update tenant request: {ex.Detail ?? ex.Title}");
        }
    }

    private async Task<ITenant> CreateChildTenantAsync(string parentTenantId, string name, Guid? wellKnownChildTenantGuid)
    {
        try
        {
            CreateChildTenantRequest body = new()
            {
                TenantName = name,
                WellKnownChildTenantGuid = wellKnownChildTenantGuid?.ToString(),
            };

            HeadersInspectionHandlerOption headersInspectionhandler = new() { InspectResponseHeaders = true };

            TenantResponse? createdTenant = await this.TenantApiClient[parentTenantId].Marain.Tenant.PostAsync(body, config => config.Options.Add(headersInspectionhandler));

            if (createdTenant == null)
            {
                throw new InvalidOperationException("Failed to create child tenant - service returned null response");
            }

            headersInspectionhandler.ResponseHeaders.TryGetValue("ETag", out IEnumerable<string>? etagValues);

            return this.TenantMapper.MapTenant(createdTenant, etagValues?.FirstOrDefault());
        }
        catch (ProblemDetails ex) when (ex.Status == 404)
        {
            throw new TenantNotFoundException();
        }
        catch (ProblemDetails ex) when (ex.Status == 409)
        {
            throw new TenantConflictException();
        }
        catch (HttpValidationProblemDetails ex) when (ex.Status == 400)
        {
            throw new ArgumentException($"Invalid create child tenant request: {ex.Detail ?? ex.Title}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create child tenant", ex);
        }
    }
}