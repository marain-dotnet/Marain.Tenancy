// <copyright file="TenantLinksResponse.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the links for a tenant in API responses.
    /// </summary>
    public sealed record TenantLinksResponse
    {
        /// <summary>
        /// Gets the link to this tenant.
        /// </summary>
        [JsonPropertyName("self")]
        public required LinkResponse Self { get; init; }

        /// <summary>
        /// Gets the link to child tenants.
        /// </summary>
        [JsonPropertyName("children")]
        public required LinkResponse Children { get; init; }
    }
}