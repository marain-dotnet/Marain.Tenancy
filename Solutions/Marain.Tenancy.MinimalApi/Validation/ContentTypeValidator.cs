// <copyright file="ContentTypeValidator.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Validation;

using System.Collections.Frozen;

/// <summary>
/// Validates content types for API operations.
/// </summary>
public static class ContentTypeValidator
{
    /// <summary>
    /// Valid content types for each HTTP method.
    /// </summary>
    private static readonly FrozenDictionary<string, FrozenSet<string>> ValidContentTypes = 
        new Dictionary<string, FrozenSet<string>>
        {
            ["POST"] = new[] { "application/json" }.ToFrozenSet(),
            ["PUT"] = new[] { "application/json" }.ToFrozenSet(),
            ["PATCH"] = new[] { "application/json-patch+json" }.ToFrozenSet()
        }.ToFrozenDictionary();

    /// <summary>
    /// Validates the content type for a given HTTP method.
    /// </summary>
    /// <param name="httpMethod">The HTTP method.</param>
    /// <param name="contentType">The content type to validate.</param>
    /// <returns>A validation result indicating success or failure.</returns>
    public static ValidationResult ValidateContentType(string httpMethod, string? contentType)
    {
        if (!ValidContentTypes.TryGetValue(httpMethod.ToUpperInvariant(), out var allowedTypes))
            return ValidationResult.Valid(); // No content type validation needed for this method

        if (string.IsNullOrWhiteSpace(contentType))
            return ValidationResult.Invalid($"Content-Type header is required for {httpMethod} requests");

        // Extract the media type part (before any parameters like charset)
        var mediaType = contentType.Split(';')[0].Trim();
        
        if (!allowedTypes.Contains(mediaType))
        {
            return ValidationResult.Invalid(
                $"Invalid Content-Type '{mediaType}' for {httpMethod}. Valid types: {string.Join(", ", allowedTypes)}");
        }

        return ValidationResult.Valid();
    }

    /// <summary>
    /// Gets the valid content types for a given HTTP method.
    /// </summary>
    /// <param name="httpMethod">The HTTP method.</param>
    /// <returns>The collection of valid content types, or empty if no validation is required.</returns>
    public static IReadOnlySet<string> GetValidContentTypes(string httpMethod)
    {
        return ValidContentTypes.TryGetValue(httpMethod.ToUpperInvariant(), out var types) 
            ? types 
            : FrozenSet<string>.Empty;
    }
}