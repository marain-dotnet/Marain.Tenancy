// <copyright file="ValidationExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Validation;

/// <summary>
/// Extension methods for validation in minimal APIs.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Adds validation using FluentValidation to the endpoint.
    /// </summary>
    /// <typeparam name="T">The type of model to validate.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The route handler builder for chaining.</returns>
    public static RouteHandlerBuilder AddValidation<T>(this RouteHandlerBuilder builder)
        where T : class
    {
        return builder.AddEndpointFilter<ValidationFilter<T>>();
    }
}