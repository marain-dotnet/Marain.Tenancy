// <copyright file="TypeRegistrar.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli;

using System;
using Spectre.Console.Cli;

/// <summary>
/// Type registrar for bridging Microsoft DI with Spectre.Console.
/// </summary>
public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeRegistrar"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public TypeRegistrar(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public ITypeResolver Build()
    {
        return new TypeResolver(this.serviceProvider);
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