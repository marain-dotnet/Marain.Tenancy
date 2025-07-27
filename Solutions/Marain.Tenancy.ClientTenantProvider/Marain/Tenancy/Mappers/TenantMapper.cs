// <copyright file="TenantMapper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Mappers;

using System;
using System.Collections.Generic;
using System.Linq;
using Corvus.Json;
using Corvus.Tenancy;
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
    public ITenant MapTenant(object source)
    {
        if (source is Client.Models.TenantResponse tenantFromService)
        {
            return new Tenant(
                tenantFromService.Id ?? string.Empty,
                tenantFromService.Name ?? string.Empty,
                this.propertyBagFactory.Create(ConvertProperties(tenantFromService.Properties)))
            {
                ETag = string.Empty, // Kiota TenantResponse doesn't have ETag property exposed
            };
        }

        // Fallback for legacy compatibility
        Client.Models.TenantResponse? tenant = ((JObject)source).ToObject<Client.Models.TenantResponse>();
        if (tenant != null)
        {
            return new Tenant(
                tenant.Id ?? string.Empty,
                tenant.Name ?? string.Empty,
                this.propertyBagFactory.Create(ConvertProperties(tenant.Properties)))
            {
                ETag = string.Empty,
            };
        }

        throw new ArgumentException("Invalid tenant source object", nameof(source));
    }

    /// <inheritdoc/>
    public Client.Models.TenantResponse MapTenant(ITenant source)
    {
        return new Client.Models.TenantResponse
        {
            Id = source.Id,
            Name = source.Name,
            ContentType = source.ContentType,
            Properties = ConvertToKiotaProperties(((JObject)source.Properties).ToObject<Dictionary<string, object>>()),
        };
    }

    private static Dictionary<string, object>? ConvertProperties(Client.Models.TenantResponse_properties? properties)
    {
        // Kiota generated properties object needs conversion
        if (properties == null)
        {
            return null;
        }

        // For now, return empty dictionary until we understand Kiota property structure
        return new Dictionary<string, object>();
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
}