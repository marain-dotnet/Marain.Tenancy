// <copyright file="ITenantMapper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Mappers;

using System;
using Corvus.Tenancy;
using Marain.Tenancy.Client.Resources;

/// <summary>
/// Maps a <see cref="TenantResource"/> to an <see cref="ITenant"/>.
/// </summary>
public interface ITenantMapper
{
    /// <summary>
    /// Map the tenant from client to SDK.
    /// </summary>
    /// <param name="source">The source model.</param>
    /// <param name="etag">The Etag header from the response.</param>
    /// <returns>The <see cref="ITenant"/>.</returns>
    /// <remarks>It is assumed this is an object which can be cast to a TenantResource.</remarks>
    ITenant MapTenant(TenantResource source, string? etag = null);

    /// <summary>
    /// Extracts a tenant ID from an absolute Marain location.
    /// </summary>
    /// <param name="absoluteUrl">The absolute Url string.</param>
    /// <returns>The tenant Id.</returns>
    string ExtractTenantIdFromAbsoluteUrl(string absoluteUrl);

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