// <copyright file="TenantResource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client.Resources;

using System.Text.Json.Serialization;
using Corvus.Json;
using Marain.Clients.Hal;

/// <summary>
/// Represents a tenant resource from the Tenancy API.
/// </summary>
public record TenantResource
{
    /// <summary>
    /// Gets the content type of the tenant resource.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the unique identifier of the tenant.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the hypermedia links associated with this tenant.
    /// </summary>
    [JsonPropertyName("_links")]
    public TenantLinksResource? Links { get; init; }

    /// <summary>
    /// Gets the display name of the tenant.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the custom properties associated with the tenant.
    /// </summary>
    public IPropertyBag? Properties { get; init; }

    /// <summary>
    /// Contains hypermedia links for tenant-related operations.
    /// </summary>
    public record TenantLinksResource
    {
        /// <summary>
        /// Gets the link to the tenant resource itself.
        /// </summary>
        public WebLink? Self { get; init; }

        /// <summary>
        /// Gets the link to the tenant's children collection.
        /// </summary>
        public WebLink? Children { get; init; }
    }
}