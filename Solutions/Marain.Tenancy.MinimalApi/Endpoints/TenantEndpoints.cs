// <copyright file="TenantEndpoints.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Endpoints;

using FluentValidation;
using Marain.Tenancy.MinimalApi.ErrorHandling;
using Marain.Tenancy.MinimalApi.Models;
using Marain.Tenancy.MinimalApi.Services;
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
    /// <param name="tenantService">The tenant service.</param>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The tenant response or problem details.</returns>
    private static async Task<Results<Ok<TenantResponse>, StatusCodeHttpResult, ProblemHttpResult>> GetTenant(
        string tenantId,
        IValidator<GetTenantParameters> validator,
        ITenantService tenantService,
        HttpContext context)
    {
        var parameters = new GetTenantParameters { TenantId = tenantId };
        var validationResult = await validator.ValidateAsync(parameters);

        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            return ErrorHandlingExtensions.BadRequestProblem(errors);
        }

        var etag = context.Request.Headers.IfNoneMatch.FirstOrDefault();
        var result = await tenantService.GetTenantAsync(tenantId, etag, context.RequestAborted);

        return result.ErrorType switch
        {
            TenantServiceErrorType.NotFound => ErrorHandlingExtensions.NotFoundProblem(result.ErrorMessage!),
            TenantServiceErrorType.NotModified => TypedResults.StatusCode(StatusCodes.Status304NotModified),
            null when result.IsSuccess => CreateOkWithETag(result.Data!, result.ETag, context),
            _ => ErrorHandlingExtensions.InternalServerErrorProblem("An unexpected error occurred")
        };
    }

    /// <summary>
    /// Creates a new child tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant identifier.</param>
    /// <param name="tenantName">The name for the new child tenant.</param>
    /// <param name="wellKnownChildTenantGuid">Optional well-known GUID for the child tenant.</param>
    /// <param name="validator">The parameter validator.</param>
    /// <param name="tenantService">The tenant service.</param>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The created tenant response or problem details.</returns>
    private static async Task<Results<Created<TenantResponse>, ProblemHttpResult>> CreateChildTenant(
        string tenantId,
        [FromQuery] string tenantName,
        [FromQuery] string? wellKnownChildTenantGuid,
        IValidator<CreateChildTenantParameters> validator,
        ITenantService tenantService,
        HttpContext context)
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

        var result = await tenantService.CreateChildTenantAsync(tenantId, tenantName, wellKnownChildTenantGuid, context.RequestAborted);

        return result.ErrorType switch
        {
            TenantServiceErrorType.NotFound => ErrorHandlingExtensions.NotFoundProblem(result.ErrorMessage!),
            TenantServiceErrorType.Conflict => ErrorHandlingExtensions.ConflictProblem(result.ErrorMessage!),
            null when result.IsSuccess => CreateCreatedWithETag(result.Data!, result.ETag, context),
            _ => ErrorHandlingExtensions.InternalServerErrorProblem("An unexpected error occurred")
        };
    }

    /// <summary>
    /// Gets child tenants for the specified parent tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant identifier.</param>
    /// <param name="continuationToken">Optional continuation token for pagination.</param>
    /// <param name="maxItems">Maximum number of items to return.</param>
    /// <param name="validator">The parameter validator.</param>
    /// <param name="tenantService">The tenant service.</param>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The child tenants response or problem details.</returns>
    private static async Task<Results<Ok<ChildTenantsResponse>, ProblemHttpResult>> GetChildTenants(
        string tenantId,
        [FromQuery] string? continuationToken,
        [FromQuery] int? maxItems,
        IValidator<GetChildrenParameters> validator,
        ITenantService tenantService,
        HttpContext context)
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

        var result = await tenantService.GetChildTenantsAsync(tenantId, maxItems, continuationToken, context.RequestAborted);

        return result.ErrorType switch
        {
            TenantServiceErrorType.NotFound => ErrorHandlingExtensions.NotFoundProblem(result.ErrorMessage!),
            null when result.IsSuccess => TypedResults.Ok(result.Data!),
            _ => ErrorHandlingExtensions.InternalServerErrorProblem("An unexpected error occurred")
        };
    }

    /// <summary>
    /// Updates a tenant using JSON Patch operations.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="context">The HTTP context.</param>
    /// <param name="validator">The parameter validator.</param>
    /// <param name="tenantService">The tenant service.</param>
    /// <returns>The updated tenant response or problem details.</returns>
    private static async Task<Results<Ok<TenantResponse>, ProblemHttpResult>> UpdateTenant(
        string tenantId,
        HttpContext context,
        IValidator<UpdateTenantParameters> validator,
        ITenantService tenantService)
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

        var jsonPatch = ((Ok<string>)jsonPatchValidation.Result).Value!;
        var result = await tenantService.UpdateTenantAsync(tenantId, jsonPatch, context.RequestAborted);

        return result.ErrorType switch
        {
            TenantServiceErrorType.NotFound => ErrorHandlingExtensions.NotFoundProblem(result.ErrorMessage!),
            TenantServiceErrorType.ValidationError => ErrorHandlingExtensions.BadRequestProblem(result.ErrorMessage!),
            null when result.IsSuccess => CreateOkWithETag(result.Data!, result.ETag, context),
            _ => ErrorHandlingExtensions.InternalServerErrorProblem("An unexpected error occurred")
        };
    }

    /// <summary>
    /// Deletes a child tenant.
    /// </summary>
    /// <param name="tenantId">The parent tenant identifier.</param>
    /// <param name="childTenantId">The child tenant identifier to delete.</param>
    /// <param name="validator">The parameter validator.</param>
    /// <param name="tenantService">The tenant service.</param>
    /// <param name="context">The HTTP context.</param>
    /// <returns>No content response or problem details.</returns>
    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteChildTenant(
        string tenantId,
        string childTenantId,
        IValidator<DeleteChildTenantParameters> validator,
        ITenantService tenantService,
        HttpContext context)
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

        var result = await tenantService.DeleteChildTenantAsync(tenantId, childTenantId, context.RequestAborted);

        return result.ErrorType switch
        {
            TenantServiceErrorType.NotFound => ErrorHandlingExtensions.NotFoundProblem(result.ErrorMessage!),
            null when result.IsSuccess => TypedResults.NoContent(),
            _ => ErrorHandlingExtensions.InternalServerErrorProblem("An unexpected error occurred")
        };
    }

    /// <summary>
    /// Creates an OK response with ETag header.
    /// </summary>
    /// <param name="tenant">The tenant response data.</param>
    /// <param name="etag">The ETag value.</param>
    /// <param name="context">The HTTP context.</param>
    /// <returns>An OK response with ETag header.</returns>
    private static Ok<TenantResponse> CreateOkWithETag(TenantResponse tenant, string? etag, HttpContext context)
    {
        var response = TypedResults.Ok(tenant);
        if (!string.IsNullOrEmpty(etag))
        {
            context.Response.Headers.ETag = etag;
        }
        return response;
    }

    /// <summary>
    /// Creates a Created response with ETag header.
    /// </summary>
    /// <param name="tenant">The tenant response data.</param>
    /// <param name="etag">The ETag value.</param>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A Created response with ETag header.</returns>
    private static Created<TenantResponse> CreateCreatedWithETag(TenantResponse tenant, string? etag, HttpContext context)
    {
        var response = TypedResults.Created($"/{tenant.Id}/marain/tenant", tenant);
        if (!string.IsNullOrEmpty(etag))
        {
            context.Response.Headers.ETag = etag;
        }
        return response;
    }
}