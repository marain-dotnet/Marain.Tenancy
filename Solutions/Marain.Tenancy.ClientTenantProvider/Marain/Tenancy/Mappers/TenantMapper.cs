// <copyright file="TenantMapper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Corvus.Extensions.Json;
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
            Client.Models.Tenant tenantFromService = ((JObject)source).ToObject<Client.Models.Tenant>();
            return new Tenant(
                tenantFromService.Id,
                tenantFromService.Name,
                this.propertyBagFactory.Create(tenantFromService.Properties))
            {
                ETag = tenantFromService.ETag,
            };
        }

        /// <inheritdoc/>
        public Client.Models.Tenant MapTenant(ITenant source)
        {
            var result = new Client.Models.Tenant
            {
                Id = source.Id,
                Name = source.Name,
                ContentType = source.ContentType,
                ETag = source.ETag,
                Properties = ((JObject)source.Properties).ToObject<Dictionary<string, object>>(),
            };
            return result;
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

            return location.Substring(offset, location.IndexOf('/', offset) - offset);
        }

        /// <inheritdoc/>
        public string ExtractContinationTokenFrom(Uri baseUri, string tokenUri)
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
}
