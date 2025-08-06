// <copyright file="EndpointNames.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Endpoints;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Names for the endpoints in the Tenancy API.
/// </summary>
public static class EndpointNames
{
    public const string GetTenant = "GetTenant";

    public const string CreateChildTenant = "CreateChildTenant";

    public const string GetChildTenants = "GetChildTenants";

    public const string UpdateTenant = "UpdateTenant";

    public const string DeleteChildTenant = "DeleteChildTenant";
}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member