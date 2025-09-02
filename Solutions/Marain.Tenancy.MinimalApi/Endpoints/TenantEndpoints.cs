// <copyright file="TenantEndpoints.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Endpoints;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Corvus.Json;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using Marain.Tenancy.MinimalApi.ErrorHandling;
using Marain.Tenancy.MinimalApi.Models;
using Marain.Tenancy.MinimalApi.Validation;
using Microsoft.AspNetCore.Http.HttpResults;

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

        group.MapPost("/", CreateChildTenant)
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
        HttpContext context)
    {
        try
        {
            string? etag = context.Request.Headers.IfNoneMatch.FirstOrDefault();
            ITenant tenant = parameters.TenantId == RootTenant.RootTenantId
                ? GetRedactedRootTenant(propertyBagFactory)
                : await tenantStore.GetTenantAsync(parameters.TenantId, etag);

            TenantResponse response = MapTenantToResponse(tenant, linkGenerator, context);

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
        [AsParameters] CreateChildTenantParameters parameters,
        ITenantStore tenantStore,
        LinkGenerator linkGenerator,
        HttpContext context)
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

            TenantResponse response = MapTenantToResponse(childTenant, linkGenerator, context);

            // Set ETag header
            if (!string.IsNullOrEmpty(childTenant.ETag))
            {
                context.Response.Headers.ETag = childTenant.ETag;
            }

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
        [AsParameters] GetChildrenParameters parameters,
        ITenantStore tenantStore,
        LinkGenerator linkGenerator,
        HttpContext context)
    {
        try
        {
            int limit = parameters.MaxItems ?? 10;
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

            return TypedResults.Ok(response);
        }
        catch (TenantNotFoundException)
        {
            return ErrorHandlingExtensions.NotFoundProblem($"Tenant with ID '{parameters.TenantId}' not found");
        }
    }

    private static async Task<Results<Ok<TenantResponse>, StatusCodeHttpResult, ProblemHttpResult>> UpdateTenant(
        [AsParameters] UpdateTenantParameters parameters,
        ITenantStore tenantStore,
        LinkGenerator linkGenerator,
        HttpContext context)
    {
        try
        {
            if (parameters.TenantId == RootTenant.RootTenantId)
            {
                return TypedResults.StatusCode(405);
            }

            string? name = null;
            Dictionary<string, object>? propertiesToSet = [];
            List<string>? propertiesToRemove = [];

            foreach (UpdateTenantJsonPatchEntry entry in parameters.UpdateTenantJsonPatchArray)
            {
                if (entry.Path == "/name")
                {
                    if ((entry.Operation == UpdateTenantJsonPatchEntryOperation.Replace) && (entry.Value is JsonElement valueElement) && (valueElement.ValueKind == JsonValueKind.String))
                    {
                        name = valueElement.GetString();
                    }
                    else
                    {
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

            ITenant updatedTenant = await tenantStore.UpdateTenantAsync(
                parameters.TenantId,
                name,
                propertiesToSet,
                propertiesToRemove);

            TenantResponse response = MapTenantToResponse(updatedTenant, linkGenerator, context);

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
        [AsParameters] DeleteChildTenantParameters parameters,
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