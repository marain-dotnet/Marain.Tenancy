// <copyright file="ChildTenantsResource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client.Resources;

using System.Collections.Generic;
using Marain.Clients.Hal;

public record ChildTenantsResource
{
    public string? ContinuationToken { get; init; }

    public int? MaxItems { get; init; }

    public ChildTenantsLinksResource? Links { get; init; }

    public record ChildTenantsLinksResource
    {
        public List<WebLink>? DeleteTenant { get; init; }

        public List<WebLink>? GetTenant { get; init; }

        public WebLink? Next { get; init; }

        public required WebLink Self { get; init; }
    }
}
