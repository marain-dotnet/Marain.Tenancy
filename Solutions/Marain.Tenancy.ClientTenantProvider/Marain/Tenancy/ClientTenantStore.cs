// <copyright file="ClientTenantStore.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
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
using Marain.Tenancy.Shared.Extensions;
using Marain.Tenancy.Shared.Telemetry;
using Microsoft.Extensions.Logging;

/// <summary>
/// An <see cref="ITenantProvider"/> built over a Marain tenancy instance.
/// </summary>
public class ClientTenantStore(
    RootTenant root,
    ITenancyClient tenancyApiClient,
    ITenantMapper tenantMapper,
    IPropertyBagFactory propertyBagFactory,
    ILogger<ClientTenantStore>? logger = null) : ClientTenantProvider(root, tenancyApiClient, tenantMapper, logger), ITenantStore
{
    // Telemetry infrastructure for store operations
    private static readonly ActivitySource StoreActivitySource = new(TelemetryConstants.BusinessActivitySource);
    private static readonly Meter StoreMeter = new(TelemetryConstants.TenancyMeter);
    private static readonly Counter<long> TenantStoreOperationsCounter =
        StoreMeter.CreateCounter<long>("tenant.store.operations.total", "operations", "Total store operations");

    private static readonly Histogram<double> TenantStoreOperationDuration =
        StoreMeter.CreateHistogram<double>("tenant.store.operation.duration", "ms", "Store operation duration");

    private static readonly Counter<long> TenantStoreErrorsCounter =
        StoreMeter.CreateCounter<long>("tenant.store.errors.total", "errors", "Total store errors");

    private readonly ILogger<ClientTenantStore>? logger = logger;

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
        using Activity? activity = StoreActivitySource.StartActivity("store.tenant.delete");
        activity?.SetTenantOperationTags(TelemetryConstants.OperationTypes.Delete, tenantId);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        this.logger?.LogInformation("Deleting tenant {TenantId} via store", tenantId);

        try
        {
            // Extract parent tenant ID from the full tenant ID path
            string parentTenantId = tenantId.GetParentId()
                ?? throw new InvalidOperationException("Unable to extract parent tenant Id from supplied tenant Id");

            await this.TenantApiClient.DeleteChildTenantAsync(parentTenantId, tenantId).ConfigureAwait(false);

            // Record successful operation
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Delete),
                new KeyValuePair<string, object?>("status", "success"));
            TenantStoreOperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);
            this.logger?.LogInformation("Successfully deleted tenant {TenantId}", tenantId);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Tenant not found");
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Delete),
                new KeyValuePair<string, object?>("error.type", "not_found"));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Delete),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogWarning("Tenant not found for deletion: {TenantId}", tenantId);
            throw new TenantNotFoundException(ex.Message, ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Bad request");
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Delete),
                new KeyValuePair<string, object?>("error.type", "bad_request"));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Delete),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogError(
                ex,
                "Bad request for tenant deletion {TenantId}: {Message}",
                tenantId,
                ex.Message);
            throw new InvalidOperationException($"Invalid delete tenant request: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Delete),
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Delete),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogError(ex, "Failed to delete tenant {TenantId}", tenantId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<TenantCollectionResult> GetChildrenAsync(string tenantId, int limit = 20, string? continuationToken = null)
    {
        using Activity? activity = StoreActivitySource.StartActivity("store.tenant.get-children");
        activity?.SetTenantOperationTags(TelemetryConstants.OperationTypes.List, tenantId);
        activity?.SetTag("limit", limit);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        this.logger?.LogDebug(
            "Getting children for tenant {TenantId} via store, limit: {Limit}",
            tenantId,
            limit);

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

            TenantCollectionResult result = new(childTenantIds, nextContinuationToken);

            // Record successful operation
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.List),
                new KeyValuePair<string, object?>("status", "success"));
            TenantStoreOperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetTag("result.count", result.Tenants.Count());
            activity?.SetStatus(ActivityStatusCode.Ok);
            this.logger?.LogDebug(
                "Successfully retrieved {Count} children for tenant {TenantId}",
                result.Tenants.Count(),
                tenantId);

            return result;
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Tenant not found");
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.List),
                new KeyValuePair<string, object?>("error.type", "not_found"));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.List),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogWarning("Tenant not found for get children: {TenantId}", tenantId);
            throw new TenantNotFoundException(ex.Message, ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Bad request");
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.List),
                new KeyValuePair<string, object?>("error.type", "bad_request"));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.List),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogError(
                ex,
                "Bad request for get children {TenantId}: {Message}",
                tenantId,
                ex.Message);
            throw new InvalidOperationException($"Invalid get children request: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.List),
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.List),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogError(ex, "Failed to get children for tenant {TenantId}", tenantId);
            throw;
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
        using Activity? activity = StoreActivitySource.StartActivity("store.tenant.update");
        activity?.SetTenantOperationTags(TelemetryConstants.OperationTypes.Update, tenantId);
        if (!string.IsNullOrEmpty(name))
        {
            activity?.SetTag(TelemetryConstants.AttributeKeys.TenantName, name);
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        this.logger?.LogInformation(
            "Updating tenant {TenantId} via store, name: {TenantName}",
            tenantId,
            name);

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

            ITenant result = this.TenantMapper.MapTenant(response.Body);

            // Record successful operation
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Update),
                new KeyValuePair<string, object?>("status", "success"));
            TenantStoreOperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);
            this.logger?.LogInformation("Successfully updated tenant {TenantId}", tenantId);

            return result;
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Tenant not found");
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Update),
                new KeyValuePair<string, object?>("error.type", "not_found"));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Update),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogWarning("Tenant not found for update: {TenantId}", tenantId);
            throw new TenantNotFoundException(ex.Message, ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.MethodNotAllowed)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Update not allowed");
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Update),
                new KeyValuePair<string, object?>("error.type", "not_allowed"));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Update),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogWarning("Tenant update not allowed: {TenantId}", tenantId);
            throw new NotSupportedException("This tenant cannot be updated", ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Bad request");
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Update),
                new KeyValuePair<string, object?>("error.type", "bad_request"));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Update),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogError(
                ex,
                "Bad request for tenant update {TenantId}: {Message}",
                tenantId,
                ex.Message);
            throw new ArgumentException($"Invalid update tenant request: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Update),
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Update),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogError(ex, "Failed to update tenant {TenantId}", tenantId);
            throw;
        }
    }

    private async Task<ITenant> CreateChildTenantAsync(string parentTenantId, string name, Guid? wellKnownChildTenantGuid)
    {
        using Activity? activity = StoreActivitySource.StartActivity("store.tenant.create-child");
        activity?.SetTenantOperationTags(TelemetryConstants.OperationTypes.Create, parentTenantId);
        activity?.SetTag(TelemetryConstants.AttributeKeys.TenantName, name);
        if (wellKnownChildTenantGuid.HasValue)
        {
            activity?.SetTag(TelemetryConstants.AttributeKeys.ChildTenantGuid, wellKnownChildTenantGuid.Value.ToString());
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        this.logger?.LogInformation(
            "Creating child tenant under {ParentTenantId} via store, name: {TenantName}, wellKnownGuid: {WellKnownGuid}",
            parentTenantId,
            name,
            wellKnownChildTenantGuid);

        try
        {
            ApiResponse<TenantResource> response = await this.TenantApiClient.CreateChildTenantAsync(
                parentTenantId,
                name,
                wellKnownChildTenantGuid?.ToString()).ConfigureAwait(false);

            response.Headers.TryGetValue("etag", out string? etag);
            ITenant result = this.TenantMapper.MapTenant(response.Body, etag);

            // Record successful operation
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Create),
                new KeyValuePair<string, object?>("status", "success"));
            TenantStoreOperationDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
            activity?.SetStatus(ActivityStatusCode.Ok);
            this.logger?.LogInformation(
                "Successfully created child tenant {TenantId} under parent {ParentTenantId}",
                result.Id,
                parentTenantId);

            return result;
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Parent tenant not found");
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Create),
                new KeyValuePair<string, object?>("error.type", "not_found"));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Create),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogWarning("Parent tenant not found for child creation: {ParentTenantId}", parentTenantId);
            throw new TenantNotFoundException(ex.Message, ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Tenant conflict");
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Create),
                new KeyValuePair<string, object?>("error.type", "conflict"));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Create),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogWarning(
                "Tenant conflict during child creation: {ParentTenantId}, wellKnownGuid: {WellKnownGuid}",
                parentTenantId,
                wellKnownChildTenantGuid);
            throw new TenantConflictException(ex.Message, ex);
        }
        catch (MarainApiException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Bad request");
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Create),
                new KeyValuePair<string, object?>("error.type", "bad_request"));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Create),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogError(
                ex,
                "Bad request for child tenant creation {ParentTenantId}: {Message}",
                parentTenantId,
                ex.Message);
            throw new ArgumentException($"Invalid create child tenant request: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            TenantStoreErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Create),
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));
            TenantStoreOperationsCounter.Add(
                1,
                new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Create),
                new KeyValuePair<string, object?>("status", "error"));

            this.logger?.LogError(ex, "Failed to create child tenant under {ParentTenantId}", parentTenantId);
            throw new InvalidOperationException("Failed to create child tenant", ex);
        }
    }
}