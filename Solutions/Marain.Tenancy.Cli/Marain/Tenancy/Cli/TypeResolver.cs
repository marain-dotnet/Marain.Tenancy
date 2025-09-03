// <copyright file="TypeResolver.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli;

using System;
using Spectre.Console.Cli;

/// <summary>
/// Type resolver for bridging Microsoft DI with Spectre.Console.
/// </summary>
public sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolver"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public TypeResolver(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public object? Resolve(Type? type)
    {
        return type is null ? null : this.serviceProvider.GetService(type);
    }
}