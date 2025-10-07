// <copyright file="ChildTenantsResource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client.Resources;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Marain.Clients.Hal;
using Marain.Tenancy.Client.Serialization;

/// <summary>
/// Represents a collection of child tenants from the Tenancy API.
/// </summary>
public record ChildTenantsResource
{
    /// <summary>
    /// Gets the continuation token for paging through results.
    /// </summary>
    public string? ContinuationToken { get; init; }

    /// <summary>
    /// Gets the maximum number of items returned in this page.
    /// </summary>
    public int? MaxItems { get; init; }

    /// <summary>
    /// Gets the hypermedia links for child tenant operations.
    /// </summary>
    [JsonPropertyName("_links")]
    public ChildTenantsLinksResource? Links { get; init; }

    /// <summary>
    /// Contains hypermedia links for child tenant operations.
    /// </summary>
    public record ChildTenantsLinksResource
    {
        /// <summary>
        /// Gets links for deleting child tenants.
        /// </summary>
        [JsonConverter(typeof(WebLinkListJsonConverter))]
        public List<WebLink>? DeleteTenant { get; init; }

        /// <summary>
        /// Gets links for retrieving individual child tenants.
        /// </summary>
        [JsonConverter(typeof(WebLinkListJsonConverter))]
        public List<WebLink>? GetTenant { get; init; }

        /// <summary>
        /// Gets the link to the next page of results (if available).
        /// </summary>
        public WebLink? Next { get; init; }

        /// <summary>
        /// Gets the link to this child tenants collection.
        /// </summary>
        public required WebLink Self { get; init; }
    }
}