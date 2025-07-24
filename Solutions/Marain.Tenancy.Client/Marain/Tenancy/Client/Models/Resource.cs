// <copyright file="ClientTenantProviderOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client.Models;

using System.Collections.Generic;
using System.Collections.Immutable;

public abstract record Resource
{
    public IImmutableDictionary<string, Resource> EmbeddedResources { get; }

    public IImmutableDictionary<string, IImmutableList<Link>> Links { get; }

    /// <summary>
    /// Instantiates a new <see cref="Resource"/> and sets the default values.
    /// </summary>
    internal Resource(IDictionary<string, Resource> embeddedResources, IDictionary<string, IImmutableList<Link>> links)
    {
        this.EmbeddedResources = embeddedResources?.ToImmutableDictionary() ?? ImmutableDictionary<string, Resource>.Empty;
        this.Links = links?.ToImmutableDictionary() ?? ImmutableDictionary<string, IImmutableList<Link>>.Empty;
    }
}
