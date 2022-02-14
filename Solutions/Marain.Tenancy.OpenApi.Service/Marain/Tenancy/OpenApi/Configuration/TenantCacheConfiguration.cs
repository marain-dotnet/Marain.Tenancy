// <copyright file="TenantCacheConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.OpenApi.Configuration
{
    /// <summary>
    /// Cache-related configuration for the tenancy service.
    /// </summary>
    public class TenantCacheConfiguration
    {
        /// <summary>
        /// Gets or sets the value that will be returned in the cache-control header.
        /// </summary>
        public string? GetTenantResponseCacheControlHeaderValue { get; set; }
    }
}