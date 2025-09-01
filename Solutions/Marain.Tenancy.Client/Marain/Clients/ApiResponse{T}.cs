// <copyright file="ApiResponse{T}.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Clients;

using System.Collections.Immutable;
using System.Net;

/// <summary>
/// A response with body from an API endpoint.
/// </summary>
/// <typeparam name="T">The type of the response body.</typeparam>
/// <param name="StatusCode">The status code of the response.</param>
/// <param name="Headers">The headers returned with the response.</param>
/// <param name="Body">The deserialized response body.</param>
public record ApiResponse<T>(HttpStatusCode StatusCode, IImmutableDictionary<string, string?> Headers, T Body)
{
}