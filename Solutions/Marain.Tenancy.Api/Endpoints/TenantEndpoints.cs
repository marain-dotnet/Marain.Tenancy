// <copyright file="TenantEndpoints.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Endpoints;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Corvus.Json;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using Marain.Tenancy.Api.Extensions.ErrorHandling;
using Marain.Tenancy.Api.Models;
using Marain.Tenancy.Api.Telemetry;
using Marain.Tenancy.Api.Validation;
using Marain.Tenancy.Shared.Telemetry;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

/// <summary>
/// Contains endpoint implementations for tenant operations.
/// </summary>
public static class TenantEndpoints
{
    /// <summary>
    /// Registers all tenant endpoints with the route group.
    /// </summary>
    /// <param name="group">The route group builder.</param>
    /// <returns>The route group builder for chaining.</returns>
    public static RouteGroupBuilder RegisterTenantEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetTenant)
            .WithName(EndpointNames.GetTenant)
            .WithSummary("Get a tenant by ID")
            .WithDescription("Retrieves detailed information about a specific tenant.")
            .Produces<TenantResponse>()
            .Produces(StatusCodes.Status304NotModified)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .AddValidation<GetTenantParameters>()
            .AddEndpointFilter<CachingEndpointFilter>();

        group.MapPost("/children", CreateChildTenant)
            .WithName(EndpointNames.CreateChildTenant)
            .WithSummary("Create a child tenant")
            .WithDescription("Creates a new child tenant under the specified parent tenant.")
            .Produces<TenantResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddValidation<CreateChildTenantParameters>();

        group.MapGet("/children", GetChildTenants)
            .WithName(EndpointNames.GetChildTenants)
            .WithSummary("Get child tenants")
            .WithDescription("Retrieves a paginated list of child tenants.")
            .Produces<ChildTenantsResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddValidation<GetChildrenParameters>();

        group.MapPatch("/", UpdateTenant)
            .WithName(EndpointNames.UpdateTenant)
            .WithSummary("Update a tenant")
            .WithDescription("Updates tenant properties using JSON Patch operations.")
            .Produces<TenantResponse>()
            .Produces(StatusCodes.Status405MethodNotAllowed)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .AddValidation<UpdateTenantParameters>();

        group.MapDelete("/children/{childTenantId}", DeleteChildTenant)
            .WithName(EndpointNames.DeleteChildTenant)
            .WithSummary("Delete a child tenant")
            .WithDescription("Deletes a child tenant and all its resources.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddValidation<DeleteChildTenantParameters>();

        return group;
    }

    private static async Task<Results<IResult, Ok<TenantResponse>, StatusCodeHttpResult, ProblemHttpResult>> GetTenant(
        [AsParameters] GetTenantParameters parameters,
        ITenantStore tenantStore,
        IPropertyBagFactory propertyBagFactory,
        LinkGenerator linkGenerator,
        HttpContext context,
        ApiTelemetryService telemetry,
        ILogger<Program> logger)
    {
        using Activity? activity = telemetry.StartTenantOperation(
            "tenant.get",
            parameters.TenantId,
            TelemetryConstants.OperationTypes.Get);

        activity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, parameters.TenantId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation("Getting tenant {TenantId}", parameters.TenantId);

            string? etag = context.Request.Headers.IfNoneMatch.FirstOrDefault();
            bool isRootTenant = parameters.TenantId == RootTenant.RootTenantId;

            activity?.SetTag("tenant.is_root", isRootTenant);
            activity?.SetTag("request.has_etag", !string.IsNullOrEmpty(etag));

            ITenant tenant = isRootTenant
                ? GetRedactedRootTenant(propertyBagFactory)
                : await tenantStore.GetTenantAsync(parameters.TenantId, etag);

            TenantResponse response = MapTenantToResponse(tenant, linkGenerator, context);

            // Set ETag header
            if (!string.IsNullOrEmpty(tenant.ETag))
            {
                context.Response.Headers.ETag = tenant.ETag;
                activity?.SetTag("response.etag_set", true);
            }

            stopwatch.Stop();
            telemetry.RecordTenantOperationSuccess(activity, TelemetryConstants.OperationTypes.Get, stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Successfully retrieved tenant {TenantId} in {Duration}ms",
                parameters.TenantId,
                stopwatch.ElapsedMilliseconds);

            return TypedResults.Ok(response);
        }
        catch (TenantNotFoundException ex)
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.Get, stopwatch.ElapsedMilliseconds, ex);

            logger.LogWarning("Tenant {TenantId} not found", parameters.TenantId);
            return ErrorHandlingExtensions.NotFoundProblem($"Tenant with ID '{parameters.TenantId}' not found");
        }
        catch (TenantNotModifiedException)
        {
            stopwatch.Stop();
            telemetry.RecordCacheHit(TelemetryConstants.OperationTypes.Get);

            activity?.SetTag("cache.hit", true);
            activity?.SetStatus(ActivityStatusCode.Ok, "Not modified - cache hit");

            logger.LogDebug(
                "Tenant {TenantId} not modified - cache hit in {Duration}ms",
                parameters.TenantId,
                stopwatch.ElapsedMilliseconds);

            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.Get, stopwatch.ElapsedMilliseconds, ex);

            logger.LogError(
                ex,
                "Error getting tenant {TenantId} after {Duration}ms",
                parameters.TenantId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static async Task<Results<Created<TenantResponse>, ProblemHttpResult>> CreateChildTenant(
        [AsParameters] CreateChildTenantParameters parameters,
        ITenantStore tenantStore,
        LinkGenerator linkGenerator,
        HttpContext context,
        ApiTelemetryService telemetry,
        ILogger<Program> logger)
    {
        using Activity? activity = telemetry.StartTenantOperation(
            "tenant.create-child",
            parameters.TenantId,
            TelemetryConstants.OperationTypes.Create);

        activity?.SetTag(TelemetryConstants.AttributeKeys.ParentTenantId, parameters.TenantId);
        activity?.SetTag(TelemetryConstants.AttributeKeys.TenantName, parameters.TenantName);

        Guid guid = Guid.Empty;
        bool isWellKnown = !string.IsNullOrEmpty(parameters.WellKnownChildTenantGuid) &&
                          Guid.TryParse(parameters.WellKnownChildTenantGuid, out guid);

        activity?.SetTag("tenant.is_well_known", isWellKnown);
        if (isWellKnown)
        {
            activity?.SetTag(TelemetryConstants.AttributeKeys.ChildTenantGuid, guid.ToString());
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation(
                "Creating child tenant {TenantName} under parent {ParentTenantId} (WellKnown: {IsWellKnown})",
                parameters.TenantName,
                parameters.TenantId,
                isWellKnown);

            ITenant childTenant;

            if (isWellKnown)
            {
                childTenant = await tenantStore.CreateWellKnownChildTenantAsync(
                    parameters.TenantId,
                    guid,
                    parameters.TenantName);
            }
            else
            {
                childTenant = await tenantStore.CreateChildTenantAsync(
                    parameters.TenantId,
                    parameters.TenantName);
            }

            TenantResponse response = MapTenantToResponse(childTenant, linkGenerator, context);

            // Set ETag header
            if (!string.IsNullOrEmpty(childTenant.ETag))
            {
                context.Response.Headers.ETag = childTenant.ETag;
                activity?.SetTag("response.etag_set", true);
            }

            activity?.SetTag("tenant.created_id", childTenant.Id);

            stopwatch.Stop();
            telemetry.RecordTenantOperationSuccess(activity, TelemetryConstants.OperationTypes.Create, stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Successfully created child tenant {TenantId} with name {TenantName} under parent {ParentTenantId} in {Duration}ms",
                childTenant.Id,
                parameters.TenantName,
                parameters.TenantId,
                stopwatch.ElapsedMilliseconds);

            return TypedResults.Created($"/{childTenant.Id}/marain/tenant", response);
        }
        catch (TenantNotFoundException ex)
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.Create, stopwatch.ElapsedMilliseconds, ex);

            logger.LogWarning(
                "Parent tenant {ParentTenantId} not found when creating child tenant {TenantName}",
                parameters.TenantId,
                parameters.TenantName);
            return ErrorHandlingExtensions.NotFoundProblem($"Parent tenant with ID '{parameters.TenantId}' not found");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("already exists"))
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.Create, stopwatch.ElapsedMilliseconds, ex);

            logger.LogWarning(
                "Conflict creating child tenant {TenantName} under parent {ParentTenantId}: {Message}",
                parameters.TenantName,
                parameters.TenantId,
                ex.Message);
            return ErrorHandlingExtensions.ConflictProblem(ex.Message);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.Create, stopwatch.ElapsedMilliseconds, ex);

            logger.LogError(
                ex,
                "Error creating child tenant {TenantName} under parent {ParentTenantId} after {Duration}ms",
                parameters.TenantName,
                parameters.TenantId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static async Task<Results<Ok<ChildTenantsResponse>, ProblemHttpResult>> GetChildTenants(
        [AsParameters] GetChildrenParameters parameters,
        ITenantStore tenantStore,
        LinkGenerator linkGenerator,
        HttpContext context,
        ApiTelemetryService telemetry,
        ILogger<Program> logger)
    {
        using Activity? activity = telemetry.StartTenantOperation(
            "tenant.get-children",
            parameters.TenantId,
            TelemetryConstants.OperationTypes.List);

        int limit = parameters.MaxItems ?? 10;
        bool hasContinuation = !string.IsNullOrEmpty(parameters.ContinuationToken);

        activity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, parameters.TenantId);
        activity?.SetTag("query.limit", limit);
        activity?.SetTag("query.has_continuation", hasContinuation);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation(
                "Getting child tenants for {TenantId} (limit: {Limit}, hasContinuation: {HasContinuation})",
                parameters.TenantId,
                limit,
                hasContinuation);

            TenantCollectionResult children = await tenantStore.GetChildrenAsync(
                parameters.TenantId,
                limit,
                parameters.ContinuationToken);

            LinkResponse selfLink = BuildGetChildrenLink(parameters.TenantId, limit, parameters.ContinuationToken, linkGenerator, context);

            LinkResponse? nextLink = string.IsNullOrEmpty(children.ContinuationToken)
                ? null
                : BuildGetChildrenLink(parameters.TenantId, limit, children.ContinuationToken, linkGenerator, context);

            IReadOnlyList<LinkResponse> getChildTenantLinks = children.Tenants.Select(
                tenantId => BuildTenantLink(tenantId, linkGenerator, context)).ToList().AsReadOnly();

            IReadOnlyList<LinkResponse> deleteChildTenantLinks = children.Tenants.Select(
                tenantId => BuildDeleteTenantLink(parameters.TenantId, tenantId, linkGenerator, context)).ToList().AsReadOnly();

            ChildTenantsResponse response = new()
            {
                ContinuationToken = children.ContinuationToken,
                MaxItems = limit,
                Links = new()
                {
                    Self = selfLink,
                    Next = nextLink,
                    DeleteTenant = deleteChildTenantLinks,
                    GetTenant = getChildTenantLinks,
                },
            };

            activity?.SetTag("result.count", children.Tenants.Count);
            activity?.SetTag("result.has_more", !string.IsNullOrEmpty(children.ContinuationToken));

            stopwatch.Stop();
            telemetry.RecordTenantOperationSuccess(activity, TelemetryConstants.OperationTypes.List, stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Successfully retrieved {Count} child tenants for {TenantId} in {Duration}ms (hasMore: {HasMore})",
                children.Tenants.Count,
                parameters.TenantId,
                stopwatch.ElapsedMilliseconds,
                !string.IsNullOrEmpty(children.ContinuationToken));

            return TypedResults.Ok(response);
        }
        catch (TenantNotFoundException ex)
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.List, stopwatch.ElapsedMilliseconds, ex);

            logger.LogWarning("Tenant {TenantId} not found when getting child tenants", parameters.TenantId);
            return ErrorHandlingExtensions.NotFoundProblem($"Tenant with ID '{parameters.TenantId}' not found");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.List, stopwatch.ElapsedMilliseconds, ex);

            logger.LogError(
                ex,
                "Error getting child tenants for {TenantId} after {Duration}ms",
                parameters.TenantId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static async Task<Results<Ok<TenantResponse>, StatusCodeHttpResult, ProblemHttpResult>> UpdateTenant(
        [AsParameters] UpdateTenantParameters parameters,
        ITenantStore tenantStore,
        LinkGenerator linkGenerator,
        HttpContext context,
        ApiTelemetryService telemetry,
        ILogger<Program> logger)
    {
        using Activity? activity = telemetry.StartTenantOperation(
            "tenant.update",
            parameters.TenantId,
            TelemetryConstants.OperationTypes.Update);

        activity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, parameters.TenantId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (parameters.TenantId == RootTenant.RootTenantId)
            {
                activity?.SetTag("tenant.is_root", true);
                activity?.SetStatus(ActivityStatusCode.Error, "Cannot update root tenant");

                logger.LogWarning("Attempted to update root tenant {TenantId}", parameters.TenantId);
                return TypedResults.StatusCode(405);
            }

            string? name = null;
            Dictionary<string, object>? propertiesToSet = [];
            List<string>? propertiesToRemove = [];

            int patchEntryCount = 0;
            bool hasNameUpdate = false;

            foreach (UpdateTenantJsonPatchEntry entry in parameters.UpdateTenantJsonPatchArray)
            {
                patchEntryCount++;

                if (entry.Path == "/name")
                {
                    hasNameUpdate = true;
                    if ((entry.Operation == UpdateTenantJsonPatchEntryOperation.Replace) && (entry.Value is JsonElement valueElement) && (valueElement.ValueKind == JsonValueKind.String))
                    {
                        name = valueElement.GetString();
                    }
                    else
                    {
                        stopwatch.Stop();
                        activity?.SetStatus(ActivityStatusCode.Error, "Invalid name patch operation");

                        logger.LogWarning("Invalid name patch operation for tenant {TenantId}", parameters.TenantId);
                        return ErrorHandlingExtensions.UnprocessableEntityProblem("\"/name\" property can only be used with op \"replace\" and \"value\" set to a string");
                    }
                }
                else if (entry.Path.StartsWith("/properties/"))
                {
                    string propertyName = entry.Path[12..];
                    switch (entry.Operation)
                    {
                        case UpdateTenantJsonPatchEntryOperation.Add:
                        case UpdateTenantJsonPatchEntryOperation.Replace:
                            propertiesToSet.Add(propertyName, entry.Value!);
                            break;

                        case UpdateTenantJsonPatchEntryOperation.Remove:
                            propertiesToRemove.Add(propertyName);
                            break;
                    }
                }
            }

            activity?.SetTag("patch.entry_count", patchEntryCount);
            activity?.SetTag("patch.has_name_update", hasNameUpdate);
            activity?.SetTag("patch.properties_to_set", propertiesToSet.Count);
            activity?.SetTag("patch.properties_to_remove", propertiesToRemove.Count);

            logger.LogInformation(
                "Updating tenant {TenantId} with {PatchCount} patch operations (name: {HasNameUpdate}, set: {SetCount}, remove: {RemoveCount})",
                parameters.TenantId,
                patchEntryCount,
                hasNameUpdate,
                propertiesToSet.Count,
                propertiesToRemove.Count);

            ITenant updatedTenant = await tenantStore.UpdateTenantAsync(
                parameters.TenantId,
                name,
                propertiesToSet,
                propertiesToRemove);

            TenantResponse response = MapTenantToResponse(updatedTenant, linkGenerator, context);

            stopwatch.Stop();
            telemetry.RecordTenantOperationSuccess(activity, TelemetryConstants.OperationTypes.Update, stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Successfully updated tenant {TenantId} in {Duration}ms",
                parameters.TenantId,
                stopwatch.ElapsedMilliseconds);

            return TypedResults.Ok(response);
        }
        catch (TenantNotFoundException ex)
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.Update, stopwatch.ElapsedMilliseconds, ex);

            logger.LogWarning("Tenant {TenantId} not found for update", parameters.TenantId);
            return ErrorHandlingExtensions.NotFoundProblem($"Tenant with ID '{parameters.TenantId}' not found");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrent modifications"))
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.Update, stopwatch.ElapsedMilliseconds, ex);

            activity?.SetTag("error.type", "concurrent_modification");
            logger.LogWarning("Concurrent modification detected for tenant {TenantId}", parameters.TenantId);
            return ErrorHandlingExtensions.ConflictProblem("The tenant was modified by another request. Please retry.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.Update, stopwatch.ElapsedMilliseconds, ex);

            logger.LogError(
                ex,
                "Error updating tenant {TenantId} after {Duration}ms",
                parameters.TenantId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteChildTenant(
        [AsParameters] DeleteChildTenantParameters parameters,
        ITenantStore tenantStore,
        ApiTelemetryService telemetry,
        ILogger<Program> logger)
    {
        using Activity? activity = telemetry.StartTenantOperation(
            "tenant.delete-child",
            parameters.ChildTenantId,
            TelemetryConstants.OperationTypes.Delete);

        activity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, parameters.ChildTenantId);
        activity?.SetTag(TelemetryConstants.AttributeKeys.ParentTenantId, parameters.TenantId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogInformation(
                "Deleting child tenant {ChildTenantId} under parent {ParentTenantId}",
                parameters.ChildTenantId,
                parameters.TenantId);

            await tenantStore.DeleteTenantAsync(parameters.ChildTenantId);

            stopwatch.Stop();
            telemetry.RecordTenantOperationSuccess(activity, TelemetryConstants.OperationTypes.Delete, stopwatch.ElapsedMilliseconds);

            logger.LogInformation(
                "Successfully deleted child tenant {ChildTenantId} under parent {ParentTenantId} in {Duration}ms",
                parameters.ChildTenantId,
                parameters.TenantId,
                stopwatch.ElapsedMilliseconds);

            return TypedResults.NoContent();
        }
        catch (TenantNotFoundException ex)
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.Delete, stopwatch.ElapsedMilliseconds, ex);

            logger.LogWarning(
                "Child tenant {ChildTenantId} not found under parent {ParentTenantId}",
                parameters.ChildTenantId,
                parameters.TenantId);
            return ErrorHandlingExtensions.NotFoundProblem($"Child tenant with ID '{parameters.ChildTenantId}' not found under parent '{parameters.TenantId}'");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("has children"))
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.Delete, stopwatch.ElapsedMilliseconds, ex);

            activity?.SetTag("error.type", "has_children");
            logger.LogWarning("Cannot delete tenant {ChildTenantId} because it has child tenants", parameters.ChildTenantId);
            return ErrorHandlingExtensions.ConflictProblem("Cannot delete tenant because it has child tenants");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            telemetry.RecordTenantOperationError(activity, TelemetryConstants.OperationTypes.Delete, stopwatch.ElapsedMilliseconds, ex);

            logger.LogError(
                ex,
                "Error deleting child tenant {ChildTenantId} under parent {ParentTenantId} after {Duration}ms",
                parameters.ChildTenantId,
                parameters.TenantId,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private static ITenant GetRedactedRootTenant(IPropertyBagFactory propertyBagFactory)
    {
        return new RedactedRootTenant(propertyBagFactory);
    }

    private static TenantResponse MapTenantToResponse(ITenant tenant, LinkGenerator linkGenerator, HttpContext context)
    {
        Dictionary<string, object> properties = [];

        return new()
        {
            Id = tenant.Id,
            Name = tenant.Name,
            ContentType = "application/vnd.marain.tenant",
            Properties = tenant.Properties, // TODO: Clone this?
            Links = new()
            {
                Self = BuildTenantLink(tenant.Id, linkGenerator, context),
                Children = BuildGetChildrenLink(tenant.Id, null, null, linkGenerator, context),
            },
        };
    }

    private static LinkResponse BuildTenantLink(string tenantId, LinkGenerator linkGenerator, HttpContext context)
    {
        string href = linkGenerator.GetPathByName(context, EndpointNames.GetTenant, new { tenantId = tenantId })
            ?? throw new InvalidOperationException($"Unable to generate self link for tenant Id {tenantId}");

        return new() { Href = href };
    }

    private static LinkResponse BuildGetChildrenLink(string tenantId, int? maxItems, string? continuationToken, LinkGenerator linkGenerator, HttpContext context)
    {
        string href = linkGenerator.GetPathByName(
            context,
            EndpointNames.GetChildTenants,
            new { tenantId, maxItems, continuationToken }) ?? throw new InvalidOperationException("Unable to generate self link for GetChildTenants");

        return new() { Href = href };
    }

    private static LinkResponse BuildDeleteTenantLink(string parentTenantId, string tenantId, LinkGenerator linkGenerator, HttpContext context)
    {
        string href = linkGenerator.GetPathByName(context, EndpointNames.DeleteChildTenant, new { tenantId = parentTenantId, childTenantId = tenantId })
            ?? throw new InvalidOperationException($"Unable to generate self link for tenant Id {tenantId}");

        return new() { Href = href };
    }

    private class RedactedRootTenant : ITenant
    {
        public RedactedRootTenant(IPropertyBagFactory propertyBagFactory)
        {
            this.Properties = propertyBagFactory.Create(PropertyBagValues.Empty);
        }

        public string Id => RootTenant.RootTenantId;

        public string Name => RootTenant.RootTenantName;

        public IPropertyBag Properties { get; }

        public string? ETag
        {
            get => RootTenant.RootTenantId;
            set => throw new NotSupportedException();
        }

        public string ContentType => Tenant.RegisteredContentType;
    }
}