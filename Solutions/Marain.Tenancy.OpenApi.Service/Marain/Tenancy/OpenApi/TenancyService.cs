// <copyright file="TenancyService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.OpenApi
{
    using System;
    using Corvus.Extensions.Json;
    using Corvus.Tenancy;
    using Menes;
    using Microsoft.ApplicationInsights;

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
        /// The operation ID for create a child tenant.
        /// </summary>
        public const string CreateChildTenantOperationId = "createChildTenant";

        /// <summary>
        /// The operation ID to delete a child tenant.
        /// </summary>
        public const string DeleteChildTenantOperationId = "deleteChildTenant";

#pragma warning disable IDE0052
        private readonly ITenantProvider tenantProvider;
        private readonly IJsonSerializerSettingsProvider serializerSettingsProvider;
        private readonly TelemetryClient telemetryClient;
#pragma warning restore IDE0052

        /// <summary>
        /// Initializes a new instance of the <see cref="TenancyService"/> class.
        /// </summary>
        /// <param name="tenantProvider">The tenant provider.</param>
        /// <param name="serializerSettingsProvider">The serializer settings provider.</param>
        /// <param name="telemetryClient">A <see cref="TelemetryClient"/> to log telemetry.</param>
        public TenancyService(
            ITenantProvider tenantProvider,
            IJsonSerializerSettingsProvider serializerSettingsProvider,
            TelemetryClient telemetryClient)
        {
            this.tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
            this.serializerSettingsProvider = serializerSettingsProvider ?? throw new ArgumentNullException(nameof(serializerSettingsProvider));
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        /* EXAMPLE
        [OperationId(CreateClaimPermissionsOperationId)]
        public async Task<OpenApiResult> CreateClaimPermissionsAsync(
            IOpenApiContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            using (this.telemetryClient.StartOperation<RequestTelemetry>(CreateClaimPermissionsOperationId))
            {
                ITenant tenant = await this.tenantProvider.GetTenantAsync(context.CurrentTenantId).ConfigureAwait(false);
                IClaimPermissionsStore store = await this.permissionsStoreFactory.GetClaimPermissionsStoreAsync(tenant).ConfigureAwait(false);
                ClaimPermissions result = await store.PersistAsync(body).ConfigureAwait(false);
                return this.OkResult(result);
            }
        }
        */
    }
}
