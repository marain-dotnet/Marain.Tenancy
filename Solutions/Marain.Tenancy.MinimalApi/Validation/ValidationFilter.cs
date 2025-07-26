// <copyright file="ValidationFilter.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Validation;

using FluentValidation;

/// <summary>
/// Endpoint filter that validates request models using FluentValidation.
/// </summary>
/// <typeparam name="T">The type of model to validate.</typeparam>
/// <param name="validator">The validator for the model type.</param>
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
    where T : class
{
    /// <summary>
    /// Validates the request model and continues the pipeline if valid.
    /// </summary>
    /// <param name="context">The endpoint filter invocation context.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <returns>A validation problem result if invalid, otherwise the result from the next filter.</returns>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        T? argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return Results.BadRequest($"Request of type {typeof(T).Name} is required");
        }

        FluentValidation.Results.ValidationResult validationResult = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!validationResult.IsValid)
        {
            var errorDict = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray());

            return Results.ValidationProblem(errorDict);
        }

        return await next(context);
    }
}