// <copyright file="TenantMapper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Mappers;

using System;
using System.Collections.Generic;
using System.Linq;
using Corvus.Json;
using Corvus.Tenancy;
using Marain.Tenancy.Client.Models;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;

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
    public ITenant MapTenant(TenantResponse source, string? etag)
    {
        return new Tenant(
            source.Id ?? string.Empty,
            source.Name ?? string.Empty,
            this.propertyBagFactory.Create(source?.Properties?.AdditionalData ?? new Dictionary<string, object>()))
        {
            ETag = etag,
        };
    }

    /// <inheritdoc/>
    public TenantResponse MapTenant(ITenant source)
    {
        return new TenantResponse
        {
            Id = source.Id,
            Name = source.Name,
            ContentType = source.ContentType,
            Properties = ConvertToKiotaProperties(((JObject)source.Properties).ToObject<Dictionary<string, object>>()),
        };
    }

    /// <inheritdoc/>
    public string ExtractTenantIdFromAbsoluteUrl(string absoluteUrl)
    {
        Uri uri = new(absoluteUrl, UriKind.Absolute);
        string path = uri.AbsolutePath;

        // Path will start with a slash. The tenant Id is the first element in the path, and will always be followed
        // by additional elements.
        return path[1..path.IndexOf('/', 1)];
    }

    /// <inheritdoc/>
    public string ExtractTenantIdFrom(Uri baseUri, string location)
    {
        int offset = 0;
        string baseUriString = baseUri.AbsoluteUri;
        if (location.StartsWith(baseUriString))
        {
            offset = baseUriString.Length;
        }

        // Remove the starting slash if present
        if (location[0] == '/')
        {
            offset += 1;
        }

        return location[offset..location.IndexOf('/', offset)];
    }

    /// <inheritdoc/>
    public string? ExtractContinationTokenFrom(Uri baseUri, string tokenUri)
    {
        if (string.IsNullOrEmpty(tokenUri))
        {
            return null;
        }

        var uri = new Uri(tokenUri, UriKind.RelativeOrAbsolute);
        if (!uri.IsAbsoluteUri)
        {
            uri = new Uri(baseUri, uri);
        }

        Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query = QueryHelpers.ParseQuery(uri.Query);
        if (!query.ContainsKey("continuationToken"))
        {
            return null;
        }

        return query["continuationToken"].FirstOrDefault();
    }

    private static Client.Models.TenantResponse_properties? ConvertToKiotaProperties(Dictionary<string, object>? properties)
    {
        if (properties == null)
        {
            return null;
        }

        // Create new Kiota properties object
        return new Client.Models.TenantResponse_properties();
    }
}