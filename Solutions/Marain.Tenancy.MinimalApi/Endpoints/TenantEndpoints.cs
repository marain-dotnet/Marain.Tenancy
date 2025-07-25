// <copyright file="TenantEndpoints.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Endpoints;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using FluentValidation;
using FluentValidation.Results;
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
        group.MapGet("/", GetTenant)
            .WithName("GetTenant")
            .WithSummary("Get a tenant by ID")
            .WithDescription("Retrieves detailed information about a specific tenant.")
            .Produces<TenantResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/", CreateChildTenant)
            .WithName("CreateChildTenant")
            .WithSummary("Create a child tenant")
            .WithDescription("Creates a new child tenant under the specified parent tenant.")
            .Produces<TenantResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/children", GetChildTenants)
            .WithName("GetChildTenants")
            .WithSummary("Get child tenants")
            .WithDescription("Retrieves a paginated list of child tenants.")
            .Produces<ChildTenantsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/", UpdateTenant)
            .WithName("UpdateTenant")
            .WithSummary("Update a tenant")
            .WithDescription("Updates tenant properties using JSON Patch operations.")
            .Produces<TenantResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/children/{childTenantId}", DeleteChildTenant)
            .WithName("DeleteChildTenant")
            .WithSummary("Delete a child tenant")
            .WithDescription("Deletes a child tenant and all its resources.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Results<Ok<TenantResponse>, StatusCodeHttpResult, ProblemHttpResult>> GetTenant(
        string tenantId,
        IValidator<GetTenantParameters> validator,
        ITenantStore tenantStore,
        HttpContext context)
    {
        var parameters = new GetTenantParameters { TenantId = tenantId };
        var validationResult = await validator.ValidateAsync(parameters, context.RequestAborted);
        if (!validationResult.IsValid)
        {
            return CreateValidationProblem(validationResult.Errors);
        }

        try
        {
            var etag = context.Request.Headers.IfNoneMatch.FirstOrDefault();
            ITenant tenant = await tenantStore.GetTenantAsync(tenantId, etag);
            
            var response = MapTenantToResponse(tenant);
            
            // Set ETag header
            if (!string.IsNullOrEmpty(tenant.ETag))
            {
                context.Response.Headers.ETag = tenant.ETag;
            }
            
            return TypedResults.Ok(response);
        }
        catch (TenantNotFoundException)
        {
            return ErrorHandlingExtensions.NotFoundProblem($"Tenant with ID '{tenantId}' not found");
        }
        catch (TenantNotModifiedException)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }
    }

    private static async Task<Results<Created<TenantResponse>, ProblemHttpResult>> CreateChildTenant(
        string tenantId,
        CreateChildTenantRequest request,
        IValidator<CreateChildTenantParameters> validator,
        ITenantStore tenantStore,
        HttpContext context)
    {
        var parameters = new CreateChildTenantParameters 
        { 
            TenantId = tenantId, 
            TenantName = request.TenantName,
            WellKnownChildTenantGuid = request.WellKnownChildTenantGuid
        };

        var validationResult = await validator.ValidateAsync(parameters, context.RequestAborted);
        if (!validationResult.IsValid)
        {
            return CreateValidationProblem(validationResult.Errors);
        }

        try
        {
            ITenant childTenant;
            
            if (!string.IsNullOrEmpty(request.WellKnownChildTenantGuid) && 
                Guid.TryParse(request.WellKnownChildTenantGuid, out Guid guid))
            {
                childTenant = await tenantStore.CreateWellKnownChildTenantAsync(
                    tenantId, 
                    guid, 
                    request.TenantName);
            }
            else
            {
                childTenant = await tenantStore.CreateChildTenantAsync(
                    tenantId, 
                    request.TenantName);
            }

            var response = MapTenantToResponse(childTenant);
            return TypedResults.Created($"/{childTenant.Id}/marain/tenant", response);
        }
        catch (TenantNotFoundException)
        {
            return ErrorHandlingExtensions.NotFoundProblem($"Parent tenant with ID '{tenantId}' not found");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("already exists"))
        {
            return ErrorHandlingExtensions.ConflictProblem(ex.Message);
        }
    }

    private static async Task<Results<Ok<ChildTenantsResponse>, ProblemHttpResult>> GetChildTenants(
        string tenantId,
        int? maxItems,
        string? continuationToken,
        IValidator<GetChildrenParameters> validator,
        ITenantStore tenantStore,
        HttpContext context)
    {
        var parameters = new GetChildrenParameters 
        { 
            TenantId = tenantId, 
            MaxItems = maxItems,
            ContinuationToken = continuationToken
        };

        var validationResult = await validator.ValidateAsync(parameters, context.RequestAborted);
        if (!validationResult.IsValid)
        {
            return CreateValidationProblem(validationResult.Errors);
        }

        try
        {
            int limit = maxItems ?? 10;
            TenantCollectionResult children = await tenantStore.GetChildrenAsync(
                tenantId, 
                limit, 
                continuationToken);

            var childTenants = new List<TenantResponse>();
            foreach (string childId in children.Tenants)
            {
                ITenant childTenant = await tenantStore.GetTenantAsync(childId, null);
                childTenants.Add(MapTenantToResponse(childTenant));
            }

            var response = new ChildTenantsResponse
            {
                Embedded = new ChildTenantsEmbedded { Tenants = childTenants },
                ContinuationToken = children.ContinuationToken
            };

            return TypedResults.Ok(response);
        }
        catch (TenantNotFoundException)
        {
            return ErrorHandlingExtensions.NotFoundProblem($"Tenant with ID '{tenantId}' not found");
        }
    }

    private static async Task<Results<Ok<TenantResponse>, ProblemHttpResult>> UpdateTenant(
        string tenantId,
        JsonPatchDocument<UpdateTenantRequest> patchDocument,
        IValidator<UpdateTenantParameters> validator,
        ITenantStore tenantStore,
        HttpContext context)
    {
        var parameters = new UpdateTenantParameters 
        { 
            TenantId = tenantId
        };

        var validationResult = await validator.ValidateAsync(parameters, context.RequestAborted);
        if (!validationResult.IsValid)
        {
            return CreateValidationProblem(validationResult.Errors);
        }

        try
        {
            // Apply patch to a temporary object to extract changes
            var tempTenant = new UpdateTenantRequest();
            patchDocument.ApplyTo(tempTenant);

            // Extract properties to update
            var propertiesToUpdate = new List<KeyValuePair<string, object>>();
            if (!string.IsNullOrEmpty(tempTenant.Description))
            {
                propertiesToUpdate.Add(new KeyValuePair<string, object>("description", tempTenant.Description));
            }

            ITenant updatedTenant = await tenantStore.UpdateTenantAsync(
                tenantId,
                tempTenant.Name,
                propertiesToUpdate,
                null);

            var response = MapTenantToResponse(updatedTenant);
            return TypedResults.Ok(response);
        }
        catch (TenantNotFoundException)
        {
            return ErrorHandlingExtensions.NotFoundProblem($"Tenant with ID '{tenantId}' not found");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrent modifications"))
        {
            return ErrorHandlingExtensions.ConflictProblem("The tenant was modified by another request. Please retry.");
        }
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteChildTenant(
        string tenantId,
        string childTenantId,
        IValidator<DeleteChildTenantParameters> validator,
        ITenantStore tenantStore,
        HttpContext context)
    {
        var parameters = new DeleteChildTenantParameters 
        { 
            TenantId = tenantId, 
            ChildTenantId = childTenantId 
        };

        var validationResult = await validator.ValidateAsync(parameters, context.RequestAborted);
        if (!validationResult.IsValid)
        {
            return CreateValidationProblem(validationResult.Errors);
        }

        try
        {
            await tenantStore.DeleteTenantAsync(childTenantId);
            return TypedResults.NoContent();
        }
        catch (TenantNotFoundException)
        {
            return ErrorHandlingExtensions.NotFoundProblem($"Child tenant with ID '{childTenantId}' not found under parent '{tenantId}'");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("has children"))
        {
            return ErrorHandlingExtensions.ConflictProblem("Cannot delete tenant because it has child tenants");
        }
    }

    private static TenantResponse MapTenantToResponse(ITenant tenant)
    {
        var properties = new Dictionary<string, object>();
        
        // Convert IPropertyBag to Dictionary<string, object>
        // We'll iterate over known property names since IPropertyBag doesn't expose Keys
        foreach (string key in new[] { "description", "parent", "created", "modified" })
        {
            if (tenant.Properties.TryGet<object>(key, out object? value) && value != null)
            {
                properties[key] = value;
            }
        }

        return new TenantResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            ContentType = "application/vnd.marain.tenant",
            Properties = properties
        };
    }

    private static ProblemHttpResult CreateValidationProblem(IEnumerable<ValidationFailure> errors)
    {
        var errorDict = errors.GroupBy(x => x.PropertyName)
                               .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());
        
        return TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Validation Error",
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            extensions: new Dictionary<string, object?> { ["errors"] = errorDict });
    }
}