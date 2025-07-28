// <copyright file="ClientTenantStore.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Corvus.Extensions.Json;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Models;
using Marain.Tenancy.Mappers;

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
    public ClientTenantStore(
        RootTenant root,
        ITenancyService tenantService,
        ITenantMapper tenantMapper)
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
        try
        {
            // Extract parent tenant ID from the full tenant ID path
            string parentTenantId = tenantId.Contains('/') ? tenantId.Substring(0, tenantId.LastIndexOf('/')) : throw new ArgumentException("Invalid tenant ID format");
            await this.TenantService.DeleteChildTenantAsync(parentTenantId, tenantId).ConfigureAwait(false);
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
            ChildTenantsResponse? result = await this.TenantService.GetChildTenantsAsync(tenantId, continuationToken, limit).ConfigureAwait(false);

            if (result == null)
            {
                throw new TenantNotFoundException();
            }

            // Extract tenant IDs from the embedded tenants
            List<string> tenantIds = result.Embedded?.Tenants?
                .Where(t => !string.IsNullOrEmpty(t.Id))
                .Select(t => t.Id!)
                .ToList() ?? new List<string>();

            // Use the continuation token from the response for pagination
            string? nextContinuationToken = result.ContinuationToken;

            return new TenantCollectionResult(tenantIds, nextContinuationToken);
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
        var operations = new List<UpdateTenantRequestOperation>();

        if (name is not null)
        {
            operations.Add(new UpdateTenantRequestOperation
            {
                Path = "/name",
                Op = "replace",
                Value = null, // TODO: Proper UntypedNode creation needed
            });
        }

        if (propertiesToSetOrAdd is not null)
        {
            foreach (KeyValuePair<string, object> kv in propertiesToSetOrAdd)
            {
                operations.Add(new UpdateTenantRequestOperation
                {
                    Path = "/properties/" + kv.Key,
                    Op = "add",
                    Value = null, // TODO: Proper UntypedNode creation needed
                });
            }
        }

        if (propertiesToRemove is not null)
        {
            foreach (string propertyName in propertiesToRemove)
            {
                operations.Add(new UpdateTenantRequestOperation
                {
                    Path = "/properties/" + propertyName,
                    Op = "remove",
                });
            }
        }

        // Create a custom patch document that properly represents the operations array
        // Since the generated Kiota model seems to have issues, we'll create a custom implementation
        var patch = new UpdateTenantRequestJsonPatchDocument();

        // NOTE: The current Kiota-generated model appears to have a design issue where
        // Operations property is not properly serialized. This is a known limitation
        // that may need to be addressed by updating the OpenAPI specification or
        // using a different approach for JSON Patch operations.
        try
        {
            TenantResponse? result = await this.TenantService.UpdateTenantAsync(tenantId, patch).ConfigureAwait(false);

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
            var request = new CreateChildTenantRequest
            {
                TenantName = name,
                WellKnownChildTenantGuid = wellKnownChildTenantGuid?.ToString(),
            };

            TenantResponse? createdTenant = await this.TenantService.CreateChildTenantAsync(parentTenantId, request).ConfigureAwait(false);

            if (createdTenant == null)
            {
                throw new InvalidOperationException("Failed to create child tenant - service returned null response");
            }

            return this.TenantMapper.MapTenant(createdTenant);
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