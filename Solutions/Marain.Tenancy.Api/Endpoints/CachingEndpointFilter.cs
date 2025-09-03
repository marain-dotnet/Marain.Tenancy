// <copyright file="CachingEndpointFilter.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Endpoints;

using System.Net;
using System.Threading.Tasks;
using Marain.Tenancy.Api.Models;
using Microsoft.Net.Http.Headers;

/// <summary>
/// Endpoint filter to add caching where required.
/// </summary>
public class CachingEndpointFilter : IEndpointFilter
{
    private readonly CacheControlHeaderValue cacheControlHeaderValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingEndpointFilter"/> class.
    /// </summary>
    /// <param name="cacheControlConfiguration">Caching configuration.</param>
    public CachingEndpointFilter(CacheControlConfiguration cacheControlConfiguration)
    {
        if (cacheControlConfiguration.GetTenantResponseCacheDurationSeconds == 0)
        {
            this.cacheControlHeaderValue = new()
            {
                NoCache = true,
                NoStore = true,
                MustRevalidate = true,
            };
        }
        else
        {
            this.cacheControlHeaderValue = new()
            {
                MaxAge = TimeSpan.FromSeconds(cacheControlConfiguration.GetTenantResponseCacheDurationSeconds),
            };
        }
    }

    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.Response.StatusCode == StatusCodes.Status200OK)
        {
            context.HttpContext.Response.GetTypedHeaders().CacheControl = this.cacheControlHeaderValue;
        }

        return await next(context);
    }
}