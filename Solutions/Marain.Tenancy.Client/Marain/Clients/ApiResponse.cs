// <copyright file="ApiResponse.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Clients;

using System.Collections.Immutable;
using System.Net;

/// <summary>
/// A response from a request to an API endpoint.
/// </summary>
/// <param name="StatusCode">The status code of the response.</param>
/// <param name="Headers">The headers returned with the response.</param>
public record ApiResponse(HttpStatusCode StatusCode, IImmutableDictionary<string, string?> Headers)
{
}