// <copyright file="TenantEndpoints.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Endpoints;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using Marain.Tenancy.MinimalApi.ErrorHandling;
using Marain.Tenancy.MinimalApi.Models;
using Marain.Tenancy.MinimalApi.Validation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

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
        group.MapGet("/", (string tenantId, ITenantStore tenantStore, HttpContext context) =>
                GetTenant(new GetTenantParameters { TenantId = tenantId }, tenantStore, context))
            .WithName("GetTenant")
            .WithSummary("Get a tenant by ID")
            .WithDescription("Retrieves detailed information about a specific tenant.")
            .Produces<TenantResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem()
            .AddValidation<GetTenantParameters>();

        group.MapPost("/", (string tenantId, CreateChildTenantRequest request, ITenantStore tenantStore) =>
                CreateChildTenant(new CreateChildTenantParameters 
                { 
                    TenantId = tenantId, 
                    TenantName = request.TenantName, 
                    WellKnownChildTenantGuid = request.WellKnownChildTenantGuid 
                }, tenantStore))
            .WithName("CreateChildTenant")
            .WithSummary("Create a child tenant")
            .WithDescription("Creates a new child tenant under the specified parent tenant.")
            .Produces<TenantResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AddValidation<CreateChildTenantParameters>();

        group.MapGet("/children", (string tenantId, int? maxItems, string? continuationToken, ITenantStore tenantStore) =>
                GetChildTenants(new GetChildrenParameters 
                { 
                    TenantId = tenantId, 
                    MaxItems = maxItems, 
                    ContinuationToken = continuationToken 
                }, tenantStore))
            .WithName("GetChildTenants")
            .WithSummary("Get child tenants")
            .WithDescription("Retrieves a paginated list of child tenants.")
            .Produces<ChildTenantsResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddValidation<GetChildrenParameters>();

        group.MapPatch("/", (string tenantId, JsonPatchDocument<UpdateTenantRequest> patchDocument, ITenantStore tenantStore) =>
                UpdateTenant(new UpdateTenantParameters { TenantId = tenantId }, patchDocument, tenantStore))
            .WithName("UpdateTenant")
            .WithSummary("Update a tenant")
            .WithDescription("Updates tenant properties using JSON Patch operations.")
            .Produces<TenantResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddValidation<UpdateTenantParameters>();

        group.MapDelete("/children/{childTenantId}", (string tenantId, string childTenantId, ITenantStore tenantStore) =>
                DeleteChildTenant(new DeleteChildTenantParameters 
                { 
                    TenantId = tenantId, 
                    ChildTenantId = childTenantId 
                }, tenantStore))
            .WithName("DeleteChildTenant")
            .WithSummary("Delete a child tenant")
            .WithDescription("Deletes a child tenant and all its resources.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddValidation<DeleteChildTenantParameters>();

        return group;
    }

    private static async Task<Results<Ok<TenantResponse>, StatusCodeHttpResult, ProblemHttpResult>> GetTenant(
        GetTenantParameters parameters,
        ITenantStore tenantStore,
        HttpContext context)
    {
        try
        {
            string? etag = context.Request.Headers.IfNoneMatch.FirstOrDefault();
            ITenant tenant = await tenantStore.GetTenantAsync(parameters.TenantId, etag);

            TenantResponse response = MapTenantToResponse(tenant);

            // Set ETag header
            if (!string.IsNullOrEmpty(tenant.ETag))
            {
                context.Response.Headers.ETag = tenant.ETag;
            }

            return TypedResults.Ok(response);
        }
        catch (TenantNotFoundException)
        {
            return ErrorHandlingExtensions.NotFoundProblem($"Tenant with ID '{parameters.TenantId}' not found");
        }
        catch (TenantNotModifiedException)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }
    }

    private static async Task<Results<Created<TenantResponse>, ProblemHttpResult>> CreateChildTenant(
        CreateChildTenantParameters parameters,
        ITenantStore tenantStore)
    {
        try
        {
            ITenant childTenant;

            if (!string.IsNullOrEmpty(parameters.WellKnownChildTenantGuid) &&
                Guid.TryParse(parameters.WellKnownChildTenantGuid, out Guid guid))
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

            TenantResponse response = MapTenantToResponse(childTenant);
            return TypedResults.Created($"/{childTenant.Id}/marain/tenant", response);
        }
        catch (TenantNotFoundException)
        {
            return ErrorHandlingExtensions.NotFoundProblem($"Parent tenant with ID '{parameters.TenantId}' not found");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("already exists"))
        {
            return ErrorHandlingExtensions.ConflictProblem(ex.Message);
        }
    }

    private static async Task<Results<Ok<ChildTenantsResponse>, ProblemHttpResult>> GetChildTenants(
        GetChildrenParameters parameters,
        ITenantStore tenantStore)
    {
        try
        {
            int limit = parameters.MaxItems ?? 10;
            TenantCollectionResult children = await tenantStore.GetChildrenAsync(
                parameters.TenantId,
                limit,
                parameters.ContinuationToken);

            List<TenantResponse> childTenants = [];
            foreach (string childId in children.Tenants)
            {
                ITenant childTenant = await tenantStore.GetTenantAsync(childId, null);
                childTenants.Add(MapTenantToResponse(childTenant));
            }

            ChildTenantsResponse response = new()
            {
                Embedded = new() { Tenants = childTenants },
                ContinuationToken = children.ContinuationToken,
            };

            return TypedResults.Ok(response);
        }
        catch (TenantNotFoundException)
        {
            return ErrorHandlingExtensions.NotFoundProblem($"Tenant with ID '{parameters.TenantId}' not found");
        }
    }

    private static async Task<Results<Ok<TenantResponse>, ProblemHttpResult>> UpdateTenant(
        UpdateTenantParameters parameters,
        JsonPatchDocument<UpdateTenantRequest> patchDocument,
        ITenantStore tenantStore)
    {
        try
        {
            // Apply patch to a temporary object to extract changes
            UpdateTenantRequest tempTenant = new();
            patchDocument.ApplyTo(tempTenant);

            // Extract properties to update
            List<KeyValuePair<string, object>> propertiesToUpdate = [];
            if (!string.IsNullOrEmpty(tempTenant.Description))
            {
                propertiesToUpdate.Add(new("description", tempTenant.Description));
            }

            ITenant updatedTenant = await tenantStore.UpdateTenantAsync(
                parameters.TenantId,
                tempTenant.Name,
                propertiesToUpdate,
                null);

            TenantResponse response = MapTenantToResponse(updatedTenant);
            return TypedResults.Ok(response);
        }
        catch (TenantNotFoundException)
        {
            return ErrorHandlingExtensions.NotFoundProblem($"Tenant with ID '{parameters.TenantId}' not found");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrent modifications"))
        {
            return ErrorHandlingExtensions.ConflictProblem("The tenant was modified by another request. Please retry.");
        }
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteChildTenant(
        DeleteChildTenantParameters parameters,
        ITenantStore tenantStore)
    {
        try
        {
            await tenantStore.DeleteTenantAsync(parameters.ChildTenantId);
            return TypedResults.NoContent();
        }
        catch (TenantNotFoundException)
        {
            return ErrorHandlingExtensions.NotFoundProblem($"Child tenant with ID '{parameters.ChildTenantId}' not found under parent '{parameters.TenantId}'");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("has children"))
        {
            return ErrorHandlingExtensions.ConflictProblem("Cannot delete tenant because it has child tenants");
        }
    }

    private static TenantResponse MapTenantToResponse(ITenant tenant)
    {
        Dictionary<string, object> properties = [];

        // Convert IPropertyBag to Dictionary<string, object>
        // We'll iterate over known property names since IPropertyBag doesn't expose Keys
        foreach (string key in new[] { "description", "parent", "created", "modified", })
        {
            if (tenant.Properties.TryGet<object>(key, out object? value) && value != null)
            {
                properties[key] = value;
            }
        }

        return new()
        {
            Id = tenant.Id,
            Name = tenant.Name,
            ContentType = "application/vnd.marain.tenant",
            Properties = properties,
        };
    }

}