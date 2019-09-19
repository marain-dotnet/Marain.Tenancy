// <copyright file="TenancyService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.OpenApi
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Corvus.Extensions.Json;
    using Corvus.Tenancy;
    using Marain.Tenancy.OpenApi.Mappers;
    using Menes;
    using Menes.Exceptions;
    using Menes.Hal;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;

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
        private readonly IJsonSerializerSettingsProvider serializerSettingsProvider;
        private readonly TelemetryClient telemetryClient;
#pragma warning restore IDE0052

        /// <summary>
        /// Initializes a new instance of the <see cref="TenancyService"/> class.
        /// </summary>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <param name="tenantMapper">The mapper from tenants to tenant resources.</param>
        /// <param name="serializerSettingsProvider">The serializer settings provider.</param>
        /// <param name="telemetryClient">A <see cref="TelemetryClient"/> to log telemetry.</param>
        public TenancyService(
            ITenantProvider tenantProvider,
            TenantMapper tenantMapper,
            IJsonSerializerSettingsProvider serializerSettingsProvider,
            TelemetryClient telemetryClient)
        {
            this.tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
            this.tenantMapper = tenantMapper ?? throw new ArgumentNullException(nameof(tenantMapper));
            this.serializerSettingsProvider = serializerSettingsProvider ?? throw new ArgumentNullException(nameof(serializerSettingsProvider));
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        /// <summary>
        /// Implements the get tenant operation.
        /// </summary>
        /// <param name="tenantId">The tenant ID.</param>
        /// <param name="context">The OpenApi context.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [OperationId(GetTenantOperationId)]
        public async Task<OpenApiResult> GetTenantAsync(
            string tenantId,
            IOpenApiContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            using (this.telemetryClient.StartOperation<RequestTelemetry>(GetTenantOperationId))
            {
                ITenant result = await this.tenantProvider.GetTenantAsync(tenantId);
                return this.OkResult(this.tenantMapper.Map(result), "application/json");
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

            if (tenantId != body.Id)
            {
                return new OpenApiResult { StatusCode = (int)HttpStatusCode.Forbidden };
            }

            if (body is null)
            {
                throw new OpenApiBadRequestException();
            }

            using (this.telemetryClient.StartOperation<RequestTelemetry>(UpdateTenantOperationId))
            {
                ITenant result = await this.tenantProvider.UpdateTenantAsync(body);

                return this.OkResult(this.tenantMapper.Map(result), "application/json");
            }
        }
    }
}
