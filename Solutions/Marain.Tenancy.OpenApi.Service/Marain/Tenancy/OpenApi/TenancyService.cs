// <copyright file="TenancyService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Corvus.Extensions.Json;
    using Corvus.Tenancy;
    using Corvus.Tenancy.Exceptions;
    using Marain.Tenancy.OpenApi.Mappers;
    using Menes;
    using Menes.Exceptions;
    using Menes.Hal;
    using Menes.Links;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     Handles claim permissions requests.
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

#pragma warning disable IDE0052
        private readonly ITenantProvider tenantProvider;
        private readonly TenantMapper tenantMapper;
        private readonly TenantCollectionResultMapper tenantCollectionResultMapper;
        private readonly IOpenApiWebLinkResolver linkResolver;
        private readonly IJsonSerializerSettingsProvider serializerSettingsProvider;
        private readonly TelemetryClient telemetryClient;
#pragma warning restore IDE0052

        /// <summary>
        /// Initializes a new instance of the <see cref="TenancyService"/> class.
        /// </summary>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <param name="tenantMapper">The mapper from tenants to tenant resources.</param>
        /// <param name="tenantCollectionResultMapper">The mapper from tenant collection results to the result resource.</param>
        /// <param name="linkResolver">The link resolver.</param>
        /// <param name="serializerSettingsProvider">The serializer settings provider.</param>
        /// <param name="telemetryClient">A <see cref="TelemetryClient"/> to log telemetry.</param>
        public TenancyService(
            ITenantProvider tenantProvider,
            TenantMapper tenantMapper,
            TenantCollectionResultMapper tenantCollectionResultMapper,
            IOpenApiWebLinkResolver linkResolver,
            IJsonSerializerSettingsProvider serializerSettingsProvider,
            TelemetryClient telemetryClient)
        {
            this.tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
            this.tenantMapper = tenantMapper ?? throw new ArgumentNullException(nameof(tenantMapper));
            this.tenantCollectionResultMapper = tenantCollectionResultMapper ?? throw new ArgumentNullException(nameof(tenantCollectionResultMapper));
            this.linkResolver = linkResolver ?? throw new ArgumentNullException(nameof(linkResolver));
            this.serializerSettingsProvider = serializerSettingsProvider ?? throw new ArgumentNullException(nameof(serializerSettingsProvider));
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        /// <summary>
        /// Implements the create tenant operation.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="context">The OpenApi context.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [OperationId(CreateChildTenantOperationId)]
        public async Task<OpenApiResult> CreateChildTenantAsync(
            string tenantId,
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

            using (this.telemetryClient.StartOperation<RequestTelemetry>(GetTenantOperationId))
            {
                try
                {
                    ITenant result = await this.tenantProvider.CreateChildTenantAsync(tenantId).ConfigureAwait(false);
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

            using (this.telemetryClient.StartOperation<RequestTelemetry>(GetTenantOperationId))
            {
                try
                {
                    TenantCollectionResult result = await this.tenantProvider.GetChildrenAsync(tenantId, maxItems ?? 20, continuationToken).ConfigureAwait(false);
                    HalDocument document = this.tenantCollectionResultMapper.Map(result);
                    if (result.ContinuationToken != null)
                    {
                        OpenApiWebLink link = maxItems.HasValue
                            ? this.linkResolver.Resolve(GetChildrenOperationId, "next", ("tenantId", tenantId), ("continuationToken", result.ContinuationToken), ("maxItems", maxItems))
                            : this.linkResolver.Resolve(GetChildrenOperationId, "next", ("tenantId", tenantId), ("continuationToken", result.ContinuationToken));
                        document.AddLink("next", link);
                    }

                    var values = new List<(string, object)> { ("tenantId", tenantId) };
                    if (maxItems.HasValue)
                    {
                        values.Add(("maxItems", maxItems));
                    }

                    if (!string.IsNullOrEmpty(continuationToken))
                    {
                        values.Add(("continuationToken", continuationToken));
                    }

                    OpenApiWebLink selfLink = this.linkResolver.Resolve(GetChildrenOperationId, "self", values.ToArray());
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
            string etag,
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

            using (this.telemetryClient.StartOperation<RequestTelemetry>(GetTenantOperationId))
            {
                try
                {
                    ITenant result = await this.tenantProvider.GetTenantAsync(tenantId, etag).ConfigureAwait(false);
                    string resultETag = result.ETag ?? GetETag(result);
                    OpenApiResult okResult = this.OkResult(this.tenantMapper.Map(result), "application/json");
                    okResult.Results.Add("ETag", resultETag);

                    return okResult;
                }
                catch (TenantNotModifiedException)
                {
                    return this.NotImplementedResult();
                }
                catch (TenantNotFoundException)
                {
                    return this.NotFoundResult();
                }
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
            ITenant body,
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

            if (tenantId != body.Id)
            {
                return this.ForbiddenResult();
            }

            if (body is null)
            {
                throw new OpenApiBadRequestException();
            }

            using (this.telemetryClient.StartOperation<RequestTelemetry>(UpdateTenantOperationId))
            {
                try
                {
                    ITenant result = await this.tenantProvider.UpdateTenantAsync(body).ConfigureAwait(false);
                    return this.OkResult(this.tenantMapper.Map(result), "application/json");
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
                return this.NotFoundResult();
            }

            using (this.telemetryClient.StartOperation<RequestTelemetry>(GetTenantOperationId))
            {
                try
                {
                    await this.tenantProvider.DeleteTenantAsync(childTenantId).ConfigureAwait(false);
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
        }

        private static string GetETag(ITenant result)
        {
            string propertiesString = ((JObject)result.Properties).ToString();
            using (var hash = MD5.Create())
            {
                return "\"" + Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(propertiesString))) + "\"";
            }
        }
    }
}
