// <copyright file="TenancyService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    using Corvus.Json;
    using Corvus.Tenancy;
    using Corvus.Tenancy.Exceptions;

    using Marain.Tenancy.OpenApi.Configuration;
    using Marain.Tenancy.OpenApi.Mappers;

    using Menes;
    using Menes.Exceptions;
    using Menes.Hal;
    using Menes.Links;

    using Microsoft.AspNetCore.JsonPatch;
    using Microsoft.AspNetCore.JsonPatch.Operations;
    using Microsoft.Extensions.Logging;

    /// <summary>
    ///     Implements the tenancy web API.
    /// </summary>
    [EmbeddedOpenApiDefinition("Marain.Tenancy.OpenApi.TenancyServices.yaml")]
    public class TenancyService : IOpenApiService
    {
        /// <summary>
        /// Uri template passed to <c>OpenApiClaimsServiceCollectionExtensions.AddRoleBasedOpenApiAccessControlWithPreemptiveExemptions</c>
        /// to distinguish between rules defining access control policy for the Tenancy service vs those for other services.
        /// </summary>
        public const string TenancyResourceTemplate = "{tenantId}/marain/tenancy/";

        /// <summary>
        /// The operation ID to get a tenant.
        /// </summary>
        public const string GetTenantOperationId = "getTenant";

        /// <summary>
        /// The operation ID to update a tenant.
        /// </summary>
        public const string UpdateTenantOperationId = "updateTenant";

        /// <summary>
        /// The operation ID to update a child tenant.
        /// </summary>
        public const string GetChildrenOperationId = "getChildren";

        /// <summary>
        /// The operation ID for create a child tenant.
        /// </summary>
        public const string CreateChildTenantOperationId = "createChildTenant";

        /// <summary>
        /// The operation ID to delete a child tenant.
        /// </summary>
        public const string DeleteChildTenantOperationId = "deleteChildTenant";

        private readonly ITenantStore tenantStore;
        private readonly TenantMapper tenantMapper;
        private readonly TenantCollectionResultMapper tenantCollectionResultMapper;
        private readonly IOpenApiWebLinkResolver linkResolver;
        private readonly TenantCacheConfiguration cacheConfiguration;
        private readonly ILogger<TenancyService> logger;
        private readonly IPropertyBagFactory propertyBagFactory;
        private ITenant? redactedRootTenant;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenancyService"/> class.
        /// </summary>
        /// <param name="tenantStore">The tenant store.</param>
        /// <param name="propertyBagFactory">Provides property bag initialization and modification services.</param>
        /// <param name="tenantMapper">The mapper from tenants to tenant resources.</param>
        /// <param name="tenantCollectionResultMapper">The mapper from tenant collection results to the result resource.</param>
        /// <param name="linkResolver">The link resolver.</param>
        /// <param name="cacheConfiguration">Cache configuration.</param>
        /// <param name="logger">The logger for the service.</param>
        public TenancyService(
            ITenantStore tenantStore,
            IPropertyBagFactory propertyBagFactory,
            TenantMapper tenantMapper,
            TenantCollectionResultMapper tenantCollectionResultMapper,
            IOpenApiWebLinkResolver linkResolver,
            TenantCacheConfiguration cacheConfiguration,
            ILogger<TenancyService> logger)
        {
            this.tenantStore = tenantStore ?? throw new ArgumentNullException(nameof(tenantStore));
            this.tenantMapper = tenantMapper ?? throw new ArgumentNullException(nameof(tenantMapper));
            this.tenantCollectionResultMapper = tenantCollectionResultMapper ?? throw new ArgumentNullException(nameof(tenantCollectionResultMapper));
            this.linkResolver = linkResolver ?? throw new ArgumentNullException(nameof(linkResolver));
            this.cacheConfiguration = cacheConfiguration ?? throw new ArgumentNullException(nameof(cacheConfiguration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.propertyBagFactory = propertyBagFactory;
        }

        /// <summary>
        /// Implements the create tenant operation.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="tenantName">The name of the new child tenant.</param>
        /// <param name="wellKnownChildTenantGuid">The well known Guid for the new tenant.</param>
        /// <param name="context">The OpenApi context.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [OperationId(CreateChildTenantOperationId)]
        public async Task<OpenApiResult> CreateChildTenantAsync(
            string tenantId,
            string tenantName,
            Guid? wellKnownChildTenantGuid,
            IOpenApiContext context)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new OpenApiBadRequestException("Bad request");
            }

            if (string.IsNullOrEmpty(tenantName))
            {
                throw new OpenApiBadRequestException("Bad request");
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                ITenant result = wellKnownChildTenantGuid.HasValue
                    ? await this.tenantStore.CreateWellKnownChildTenantAsync(tenantId, wellKnownChildTenantGuid.Value, tenantName).ConfigureAwait(false)
                    : await this.tenantStore.CreateChildTenantAsync(tenantId, tenantName).ConfigureAwait(false);

                return this.CreatedResult(this.linkResolver, GetTenantOperationId, ("tenantId", result.Id));
            }
            catch (TenantNotFoundException)
            {
                return this.NotFoundResult();
            }
            catch (TenantConflictException)
            {
                return this.ConflictResult();
            }
            catch (ArgumentException)
            {
                return new OpenApiResult { StatusCode = 400 };
            }
        }

        /// <summary>
        /// Implements the get tenant children operation.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="maxItems">The maximum number of items.</param>
        /// <param name="continuationToken">The continuation token.</param>
        /// <param name="context">The OpenApi context.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [OperationId(GetChildrenOperationId)]
        public async Task<OpenApiResult> GetChildrenAsync(
            string tenantId,
            int? maxItems,
            string continuationToken,
            IOpenApiContext context)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new OpenApiBadRequestException("Bad request");
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                TenantCollectionResult result = await this.tenantStore.GetChildrenAsync(tenantId, maxItems ?? 20, continuationToken).ConfigureAwait(false);
                HalDocument document = await this.tenantCollectionResultMapper.MapAsync(result).ConfigureAwait(false);
                if (result.ContinuationToken != null)
                {
                    OpenApiWebLink link = maxItems.HasValue
                        ? this.linkResolver.ResolveByOperationIdAndRelationType(GetChildrenOperationId, "next", ("tenantId", tenantId), ("continuationToken", result.ContinuationToken), ("maxItems", maxItems))
                        : this.linkResolver.ResolveByOperationIdAndRelationType(GetChildrenOperationId, "next", ("tenantId", tenantId), ("continuationToken", result.ContinuationToken));
                    document.AddLink("next", link);
                }

                var values = new List<(string, object?)> { ("tenantId", tenantId) };
                if (maxItems.HasValue)
                {
                    values.Add(("maxItems", maxItems));
                }

                if (!string.IsNullOrEmpty(continuationToken))
                {
                    values.Add(("continuationToken", continuationToken));
                }

                OpenApiWebLink selfLink = this.linkResolver.ResolveByOperationIdAndRelationType(GetChildrenOperationId, "self", values.ToArray());
                document.AddLink("self", selfLink);

                return this.OkResult(document, "application/json");
            }
            catch (TenantNotFoundException)
            {
                return this.NotFoundResult();
            }
            catch (TenantConflictException)
            {
                return this.ConflictResult();
            }
        }

        /// <summary>
        /// Implements the get tenant operation.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="etag">The etag of the existing version.</param>
        /// <param name="context">The OpenApi context.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [OperationId(GetTenantOperationId)]
        public async Task<OpenApiResult> GetTenantAsync(
            string tenantId,
            [OpenApiParameter("If-None-Match")]
            string? etag,
            IOpenApiContext context)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new OpenApiBadRequestException("Bad request");
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                ITenant result = tenantId == RootTenant.RootTenantId
                    ? this.GetRedactedRootTenant()
                    : result = await this.tenantStore.GetTenantAsync(tenantId, etag).ConfigureAwait(false);

                OpenApiResult okResult = this.OkResult(await this.tenantMapper.MapAsync(result).ConfigureAwait(false), "application/json");

                if (!string.IsNullOrEmpty(result.ETag))
                {
                    okResult.Results.Add("ETag", result.ETag!);
                }

                if (!string.IsNullOrEmpty(this.cacheConfiguration.GetTenantResponseCacheControlHeaderValue))
                {
                    okResult.Results.Add("Cache-Control", this.cacheConfiguration.GetTenantResponseCacheControlHeaderValue!);
                }
                else
                {
                    this.logger.LogWarning("Tenancy cache configuration does not contain a GetTenantResponseCacheControlHeaderValue so no cache header will be sent on the returned tenant.");
                }

                return okResult;
            }
            catch (TenantNotModifiedException)
            {
                return this.NotModifiedResult();
            }
            catch (TenantNotFoundException)
            {
                return this.NotFoundResult();
            }
        }

        /// <summary>
        /// Implements the update tenant operation.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="body">The tenant to update.</param>
        /// <param name="context">The OpenApi context.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [OperationId(UpdateTenantOperationId)]
        public async Task<OpenApiResult> UpdateTenantAsync(
            string tenantId,
            JsonPatchDocument body,
            IOpenApiContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new OpenApiBadRequestException("Bad request");
            }

            if (body is null)
            {
                throw new OpenApiBadRequestException();
            }

            if (tenantId == RootTenant.RootTenantId)
            {
                // Updates to the root tenant are blocked because in Marain services, the root
                // tenant is locally synthesized, and not fetched from the tenancy service.
                // This enables service-specific fallback settings to be configured on the root.
                // But it also means services will never see settings configured on the root
                // via the Marain.Tenancy service, and so, to avoid disappointment, we don't
                // let anyone try to do this.
                return new OpenApiResult { StatusCode = (int)HttpStatusCode.MethodNotAllowed };
            }

            try
            {
                string? name = null;
                Dictionary<string, object>? propertiesToSet = null;
                List<string>? propertiesToRemove = null;

                foreach (Operation operation in body.Operations)
                {
                    if (operation.path == "/name")
                    {
                        if (operation.OperationType == OperationType.Replace &&
                            operation.value is string newTenantName)
                        {
                            name = newTenantName;
                        }
                        else
                        {
                            return new OpenApiResult { StatusCode = 422 };  // Unprocessable entity
                        }
                    }
                    else
                    {
                        if (operation.path.StartsWith("/properties/"))
                        {
                            string propertyName = operation.path[12..];
                            switch (operation.OperationType)
                            {
                                case OperationType.Add:
                                case OperationType.Replace:
                                    (propertiesToSet ??= new Dictionary<string, object>()).Add(propertyName, operation.value);
                                    break;

                                case OperationType.Remove:
                                    (propertiesToRemove ??= new List<string>()).Add(propertyName);
                                    break;
                            }
                        }
                    }
                }

                ITenant result = await this.tenantStore.UpdateTenantAsync(
                    tenantId,
                    name,
                    propertiesToSet,
                    propertiesToRemove)
                    .ConfigureAwait(false);

                return this.OkResult(
                    await this.tenantMapper
                        .MapAsync(result)
                        .ConfigureAwait(false), "application/json");
            }
            catch (InvalidOperationException)
            {
                // You are not allowed to update the Root Tenant
                return this.ForbiddenResult();
            }
            catch (TenantNotFoundException)
            {
                return this.NotFoundResult();
            }
        }

        /// <summary>
        /// Implements the delete tenant operation.
        /// </summary>
        /// <param name="tenantId">The parent tenant ID.</param>
        /// <param name="childTenantId">The child tenant ID.</param>
        /// <param name="context">The OpenApi context.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [OperationId(DeleteChildTenantOperationId)]
        public async Task<OpenApiResult> DeleteChildTenantAsync(
            string tenantId,
            string childTenantId,
            IOpenApiContext context)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new OpenApiBadRequestException("Bad request");
            }

            if (childTenantId is null)
            {
                throw new OpenApiBadRequestException("Bad request");
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (childTenantId.GetParentId() != tenantId)
            {
                this.logger.LogError(
                    "The discovered parent tenant ID {discoveredParentTenantId} of the child {childTenantId} does not match the specified parent {specifiedParentTenantId}",
                    childTenantId.GetParentId(),
                    childTenantId,
                    tenantId);
                throw new OpenApiNotFoundException();
            }

            try
            {
                this.logger.LogInformation("Attempting to delete {childTenantId}", childTenantId);
                await this.tenantStore.DeleteTenantAsync(childTenantId).ConfigureAwait(false);
                return this.OkResult();
            }
            catch (TenantNotFoundException)
            {
                return this.NotFoundResult();
            }
            catch (TenantConflictException)
            {
                return this.ConflictResult();
            }
        }

        private ITenant GetRedactedRootTenant() => this.redactedRootTenant ??= new RedactedRootTenant(this.propertyBagFactory);

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
                get => null;
                set => throw new NotSupportedException();
            }

            public string ContentType => Tenant.RegisteredContentType;
        }
    }
}
