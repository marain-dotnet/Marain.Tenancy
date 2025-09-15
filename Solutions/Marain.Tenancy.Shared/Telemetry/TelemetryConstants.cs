// <copyright file="TelemetryConstants.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Shared.Telemetry;

/// <summary>
/// Constants for telemetry configuration across the Marain.Tenancy solution.
/// </summary>
public static class TelemetryConstants
{
    /// <summary>
    /// The service name for the Marain Tenancy solution.
    /// </summary>
    public const string ServiceName = "Marain.Tenancy";

    /// <summary>
    /// The service version for telemetry.
    /// </summary>
    public const string ServiceVersion = "1.0.0";

    /// <summary>
    /// Activity source name for the API layer.
    /// </summary>
    public const string ApiActivitySource = "Marain.Tenancy.Api";

    /// <summary>
    /// Activity source name for the CLI application.
    /// </summary>
    public const string CliActivitySource = "Marain.Tenancy.Cli";

    /// <summary>
    /// Activity source name for the client SDK.
    /// </summary>
    public const string ClientActivitySource = "Marain.Tenancy.Client";

    /// <summary>
    /// Activity source name for business logic operations.
    /// </summary>
    public const string BusinessActivitySource = "Marain.Tenancy.Business";

    /// <summary>
    /// Activity source name for storage layer operations.
    /// </summary>
    public const string StorageActivitySource = "Marain.Tenancy.Storage";

    /// <summary>
    /// Meter name for custom Tenancy metrics.
    /// </summary>
    public const string TenancyMeter = "Marain.Tenancy";

    /// <summary>
    /// Common telemetry attribute keys.
    /// </summary>
    public static class AttributeKeys
    {
        /// <summary>
        /// Tenant ID attribute key.
        /// </summary>
        public const string TenantId = "tenant.id";

        /// <summary>
        /// Operation type attribute key.
        /// </summary>
        public const string OperationType = "operation.type";

        /// <summary>
        /// Command type attribute key for CLI operations.
        /// </summary>
        public const string CommandType = "command.type";

        /// <summary>
        /// Storage operation attribute key.
        /// </summary>
        public const string StorageOperation = "storage.operation";

        /// <summary>
        /// Azure container attribute key.
        /// </summary>
        public const string AzureContainer = "azure.container";

        /// <summary>
        /// Parent tenant ID attribute key.
        /// </summary>
        public const string ParentTenantId = "tenant.parent.id";

        /// <summary>
        /// Child tenant GUID attribute key.
        /// </summary>
        public const string ChildTenantGuid = "tenant.child.guid";

        /// <summary>
        /// Tenant name attribute key.
        /// </summary>
        public const string TenantName = "tenant.name";
    }

    /// <summary>
    /// Common operation type values.
    /// </summary>
    public static class OperationTypes
    {
        /// <summary>
        /// Create operation type.
        /// </summary>
        public const string Create = "create";

        /// <summary>
        /// Get operation type.
        /// </summary>
        public const string Get = "get";

        /// <summary>
        /// Update operation type.
        /// </summary>
        public const string Update = "update";

        /// <summary>
        /// Delete operation type.
        /// </summary>
        public const string Delete = "delete";

        /// <summary>
        /// List operation type.
        /// </summary>
        public const string List = "list";

        /// <summary>
        /// Tenant operation type.
        /// </summary>
        public const string Tenant = "tenant";

        /// <summary>
        /// Storage operation type.
        /// </summary>
        public const string Storage = "storage";
    }
}