// <copyright file="TenantEndpoints.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Endpoints;

using FluentValidation;
using Marain.Tenancy.MinimalApi.ErrorHandling;
using Marain.Tenancy.MinimalApi.Models;
using Marain.Tenancy.MinimalApi.Validation;
using Microsoft.AspNetCore.Http.HttpResults;
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
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType);

        group.MapDelete("/children/{childTenantId}", DeleteChildTenant)
            .WithName("DeleteChildTenant")
            .WithSummary("Delete a child tenant")
            .WithDescription("Deletes the specified child tenant.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    /// <summary>
    /// Gets a tenant by its identifier.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="validator">The parameter validator.</param>
    /// <returns>The tenant response or problem details.</returns>
    private static async Task<Results<Ok<TenantResponse>, ProblemHttpResult>> GetTenant(
        string tenantId,
        IValidator<GetTenantParameters> validator)
    {
        var parameters = new GetTenantParameters { TenantId = tenantId };
        var validationResult = await validator.ValidateAsync(parameters);

        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return ErrorHandlingExtensions.BadRequestProblem(errors);
        }

        // TODO: Implement actual tenant retrieval logic
        var tenant = new TenantResponse
        {
            Id = tenantId,
            Name = $"Tenant-{tenantId}",
            ContentType = "application/vnd.marain.tenant"
        };

        return TypedResults.Ok(tenant);
    }

    /// <summary>
    /// Creates a new child tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant identifier.</param>
    /// <param name="tenantName">The name for the new child tenant.</param>
    /// <param name="wellKnownChildTenantGuid">Optional well-known GUID for the child tenant.</param>
    /// <param name="validator">The parameter validator.</param>
    /// <returns>The created tenant response or problem details.</returns>
    private static async Task<Results<Created<TenantResponse>, ProblemHttpResult>> CreateChildTenant(
        string tenantId,
        [FromQuery] string tenantName,
        [FromQuery] string? wellKnownChildTenantGuid,
        IValidator<CreateChildTenantParameters> validator)
    {
        var parameters = new CreateChildTenantParameters
        {
            TenantId = tenantId,
            TenantName = tenantName,
            WellKnownChildTenantGuid = wellKnownChildTenantGuid
        };

        var validationResult = await validator.ValidateAsync(parameters);

        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return ErrorHandlingExtensions.BadRequestProblem(errors);
        }

        // TODO: Implement actual child tenant creation logic
        var childTenantId = wellKnownChildTenantGuid ?? Guid.NewGuid().ToString();
        var childTenant = new TenantResponse
        {
            Id = childTenantId,
            Name = tenantName,
            ContentType = "application/vnd.marain.tenant"
        };

        return TypedResults.Created($"/{tenantId}/marain/tenant", childTenant);
    }

    /// <summary>
    /// Gets child tenants for the specified parent tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant identifier.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <param name="maxItems">Maximum number of items to return.</param>
    /// <param name="validator">The parameter validator.</param>
    /// <returns>The child tenants response or problem details.</returns>
    private static async Task<Results<Ok<ChildTenantsResponse>, ProblemHttpResult>> GetChildTenants(
        string tenantId,
        [FromQuery] string? continuationToken,
        [FromQuery] int? maxItems,
        IValidator<GetChildrenParameters> validator)
    {
        var parameters = new GetChildrenParameters
        {
            TenantId = tenantId,
            ContinuationToken = continuationToken,
            MaxItems = maxItems
        };

        var validationResult = await validator.ValidateAsync(parameters);

        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return ErrorHandlingExtensions.BadRequestProblem(errors);
        }

        // TODO: Implement actual child tenant retrieval logic
        var response = new ChildTenantsResponse
        {
            Embedded = new ChildTenantsEmbedded
            {
                Tenants = []
            },
            ContinuationToken = null
        };

        return TypedResults.Ok(response);
    }

    /// <summary>
    /// Updates a tenant using JSON Patch operations.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="context">The HTTP context.</param>
    /// <param name="validator">The parameter validator.</param>
    /// <returns>The updated tenant response or problem details.</returns>
    private static async Task<Results<Ok<TenantResponse>, ProblemHttpResult>> UpdateTenant(
        string tenantId,
        HttpContext context,
        IValidator<UpdateTenantParameters> validator)
    {
        var contentTypeValidation = context.ValidateContentType();
        if (contentTypeValidation.Result is BadRequest<string>)
        {
            return ErrorHandlingExtensions.UnsupportedMediaTypeProblem("Invalid content type for PATCH request");
        }

        var jsonPatchValidation = await context.ValidateJsonPatchAsync();
        if (jsonPatchValidation.Result is BadRequest<string> badRequest)
        {
            return ErrorHandlingExtensions.BadRequestProblem(badRequest.Value!);
        }

        var parameters = new UpdateTenantParameters { TenantId = tenantId };
        var validationResult = await validator.ValidateAsync(parameters);

        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return ErrorHandlingExtensions.BadRequestProblem(errors);
        }

        // TODO: Implement actual tenant update logic
        var tenant = new TenantResponse
        {
            Id = tenantId,
            Name = $"Updated-Tenant-{tenantId}",
            ContentType = "application/vnd.marain.tenant"
        };

        return TypedResults.Ok(tenant);
    }

    /// <summary>
    /// Deletes a child tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant identifier.</param>
    /// <param name="childTenantId">The child tenant identifier to delete.</param>
    /// <param name="validator">The parameter validator.</param>
    /// <returns>No content response or problem details.</returns>
    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteChildTenant(
        string tenantId,
        string childTenantId,
        IValidator<DeleteChildTenantParameters> validator)
    {
        var parameters = new DeleteChildTenantParameters
        {
            TenantId = tenantId,
            ChildTenantId = childTenantId
        };

        var validationResult = await validator.ValidateAsync(parameters);

        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return ErrorHandlingExtensions.BadRequestProblem(errors);
        }

        // TODO: Implement actual child tenant deletion logic

        return TypedResults.NoContent();
    }
}