// <copyright file="ITenantMapper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Mappers
{
    using System;
    using Corvus.Tenancy;

    /// <summary>
    /// Maps a <see cref="Client.Models.Tenant"/> to an <see cref="ITenant"/>.
    /// </summary>
    public interface ITenantMapper
    {
        /// <summary>
        /// Map the tenant from client to SDK.
        /// </summary>
        /// <param name="source">The source model.</param>
        /// <returns>The <see cref="ITenant"/>.</returns>
        /// <remarks>It is assumed this is an object which can be cast to a JObject.</remarks>
        ITenant MapTenant(object source);

        /// <summary>
        /// Map the tenant from SDK to client.
        /// </summary>
        /// <param name="source">The source <see cref="ITenant"/>.</param>
        /// <returns>The <see cref="Client.Models.Tenant"/>.</returns>
        Client.Models.Tenant MapTenant(ITenant source);

        /// <summary>
        /// Extracts a tenant ID from a Marain location.
        /// </summary>
        /// <param name="baseUri">The base URI for the service.</param>
        /// <param name="location">The location string.</param>
        /// <returns>The tenant ID.</returns>
        string ExtractTenantIdFrom(Uri baseUri, string location);

        /// <summary>
        /// Extracts a continuation token from a Marain URI.
        /// </summary>
        /// <param name="baseUri">The base URI for the service.</param>
        /// <param name="tokenUri">The uri containing the contination token.</param>
        /// <returns>The tenant ID.</returns>
        /// <remarks>The continuation token should be in the <c>?continuationToken={}</c> parameter.</remarks>
        string? ExtractContinationTokenFrom(Uri baseUri, string tokenUri);
    }
}