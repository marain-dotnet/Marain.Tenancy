// <copyright file="TypeRegistrar.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli;

using System;
using Spectre.Console.Cli;

/// <summary>
/// Type registrar for bridging Microsoft DI with Spectre.Console.
/// </summary>
public sealed class TypeRegistrar(IServiceProvider serviceProvider) : ITypeRegistrar
{
    /// <inheritdoc />
    public ITypeResolver Build()
    {
        return new TypeResolver(serviceProvider);
    }

    /// <inheritdoc />
    public void Register(Type service, Type implementation)
    {
        // This method is not used when providing an existing service provider
    }

    /// <inheritdoc />
    public void RegisterInstance(Type service, object implementation)
    {
        // This method is not used when providing an existing service provider
    }

    /// <inheritdoc />
    public void RegisterLazy(Type service, Func<object> factory)
    {
        // This method is not used when providing an existing service provider
    }
}