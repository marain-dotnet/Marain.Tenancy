// <copyright file="ValidationResult.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed record ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="isValid">Whether the validation passed.</param>
    /// <param name="errorMessage">The error message if validation failed.</param>
    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        this.IsValid = isValid;
        this.ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a valid validation result.
    /// </summary>
    /// <returns>A valid validation result.</returns>
    public static ValidationResult Valid() => new(true);

    /// <summary>
    /// Creates an invalid validation result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>An invalid validation result.</returns>
    public static ValidationResult Invalid(string errorMessage) => new(false, errorMessage);
}