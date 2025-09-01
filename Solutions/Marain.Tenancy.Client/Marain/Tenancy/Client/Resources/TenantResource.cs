// <copyright file="TenantResource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client.Resources;

using Corvus.Json;
using Marain.Clients.Hal;

public record TenantResource
{
    public string? ContentType { get; init; }
    
    public required string Id { get; init; }
    
    public TenantLinksResource? Links { get; init; }
    
    public required string Name { get; init; }
    
    public IPropertyBag? Properties { get; init; }

    public record TenantLinksResource
    {
        public WebLink? Self { get; init; }

        public WebLink? Children { get; init; }
    }
}
