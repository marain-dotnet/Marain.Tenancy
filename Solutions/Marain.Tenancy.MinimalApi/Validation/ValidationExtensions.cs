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
    /// Validates a model using FluentValidation and returns appropriate HTTP responses.
    /// </summary>
    /// <typeparam name="T">The type of model to validate.</typeparam>
    /// <param name="model">The model to validate.</param>
    /// <param name="validator">The validator to use.</param>
    /// <returns>A validation result with either success or error details.</returns>
    public static async Task<Results<Ok<T>, BadRequest<string>>> ValidateAsync<T>(
        this T model, 
        IValidator<T> validator)
        where T : notnull
    {
        var validationResult = await validator.ValidateAsync(model);
        
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
        var method = context.Request.Method;
        var contentType = context.Request.ContentType;
        
        var validationResult = ContentTypeValidator.ValidateContentType(method, contentType);
        
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
        using var reader = new StreamReader(context.Request.Body);
        var jsonPatch = await reader.ReadToEndAsync();
        
        var validationResult = JsonPatchValidator.ValidateJsonPatch(jsonPatch);
        
        return validationResult.IsValid
            ? TypedResults.Ok(jsonPatch)
            : TypedResults.BadRequest(validationResult.ErrorMessage!);
    }
}