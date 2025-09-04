// <copyright file="TypeResolver.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli;

using System;
using Spectre.Console.Cli;

/// <summary>
/// Type resolver for bridging Microsoft DI with Spectre.Console.
/// </summary>
public sealed class TypeResolver(IServiceProvider serviceProvider) : ITypeResolver
{
    /// <inheritdoc />
    public object? Resolve(Type? type)
    {
        return type is null ? null : serviceProvider.GetService(type);
    }
}