// <copyright file="ValidationExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Validation;

using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;

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

    /// <summary>
    /// Validates a model using FluentValidation and returns appropriate HTTP responses.
    /// </summary>
    /// <typeparam name="T">The type of model to validate.</typeparam>
    /// <param name="model">The model to validate.</param>
    /// <param name="validator">The validator to use.</param>
    /// <returns>A validation result with either success or error details.</returns>
    [Obsolete("Use AddValidation<T>() endpoint filter instead for better separation of concerns")]
    public static async Task<Results<Ok<T>, BadRequest<string>>> ValidateAsync<T>(
        this T model,
        IValidator<T> validator)
        where T : notnull
    {
        FluentValidation.Results.ValidationResult validationResult = await validator.ValidateAsync(model);

        return validationResult.IsValid
            ? TypedResults.Ok(model)
            : TypedResults.BadRequest(string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
    }

    /// <summary>
    /// Validates content type for the current HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A validation result indicating success or failure.</returns>
    public static Results<Ok, BadRequest<string>> ValidateContentType(this HttpContext context)
    {
        string method = context.Request.Method;
        string? contentType = context.Request.ContentType;

        ValidationResult validationResult = ContentTypeValidator.ValidateContentType(method, contentType);

        return validationResult.IsValid
            ? TypedResults.Ok()
            : TypedResults.BadRequest(validationResult.ErrorMessage!);
    }

    /// <summary>
    /// Validates a JSON Patch document from the request body.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A validation result for the JSON Patch document.</returns>
    public static async Task<Results<Ok<string>, BadRequest<string>>> ValidateJsonPatchAsync(this HttpContext context)
    {
        using StreamReader reader = new(context.Request.Body);
        string jsonPatch = await reader.ReadToEndAsync();

        ValidationResult validationResult = JsonPatchValidator.ValidateJsonPatch(jsonPatch);

        return validationResult.IsValid
            ? TypedResults.Ok(jsonPatch)
            : TypedResults.BadRequest(validationResult.ErrorMessage!);
    }
}