// <copyright file="TenancyApiClientConfiguration.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy;

public class TenancyApiClientConfiguration
{
    /// <summary>
    /// Gets the base Url of the API.
    /// </summary>
    public required string BaseUri { get; init; }

    /// <summary>
    /// Gets the resource Id to use when authenticating.
    /// </summary>
    public string? ResourceIdForMsiAuthentication { get; init; }
}
