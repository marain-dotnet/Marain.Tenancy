// <copyright file="TenantMapper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Mappers;

using System;
using System.Collections.Generic;
using System.Linq;
using Corvus.Json;
using Corvus.Tenancy;
using Marain.Tenancy.Client.Resources;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

/// <summary>
/// Maps a client tenant to an API tenant.
/// </summary>
public class TenantMapper : ITenantMapper
{
    private readonly IPropertyBagFactory propertyBagFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantMapper"/> class.
    /// </summary>
    /// <param name="propertyBagFactory">Enables property bag building.</param>
    public TenantMapper(
        IPropertyBagFactory propertyBagFactory)
    {
        this.propertyBagFactory = propertyBagFactory ?? throw new ArgumentNullException(nameof(propertyBagFactory));
    }

    /// <inheritdoc/>
    public ITenant MapTenant(TenantResource source, string? etag)
    {
        return new Tenant(
            source.Id ?? string.Empty,
            source.Name ?? string.Empty,
            source.Properties ?? this.propertyBagFactory.Create(builder => builder))
        {
            ETag = etag,
        };
    }

    /// <inheritdoc/>
    public string ExtractTenantIdFromUrlPath(string path)
    {
        if (!path.StartsWith("/"))
        {
            throw new ArgumentException($"Url paths should start with a slash. The supplied path, [{path}], does not.");
        }

        // Path starts with a slash. The tenant Id is the first element in the path, and will always be followed
        // by additional elements.
        return path[1..path.IndexOf('/', 1)];
    }

    /// <inheritdoc/>
    public string? ExtractContinationTokenFromUrlPathAndQuery(string pathAndQuery)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(pathAndQuery));
        if (!pathAndQuery.StartsWith("/"))
        {
            throw new ArgumentException($"Url paths should start with a slash. The supplied path, [{pathAndQuery}], does not.");
        }

        int queryIndex = pathAndQuery.IndexOf("/");

        if (queryIndex < 0)
        {
            return null;
        }

        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(pathAndQuery[(queryIndex + 1)..]);
        if (!query.TryGetValue("continuationToken", out StringValues value))
        {
            return null;
        }

        return value.FirstOrDefault();
    }
}