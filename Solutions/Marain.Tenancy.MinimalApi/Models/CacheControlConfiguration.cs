// <copyright file="CacheControlConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Models;

/// <summary>
/// Caching configuration.
/// </summary>
public class CacheControlConfiguration
{
    /// <summary>
    /// Gets or sets the number of seconds to use when setting caching headers for the Get Tenant endpoint.
    /// </summary>
    public int GetTenantResponseCacheDurationSeconds { get; set; }
}