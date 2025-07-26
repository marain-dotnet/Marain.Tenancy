// <copyright file="CreateChildTenantParametersValidator.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Validation;

using FluentValidation;
using Marain.Tenancy.MinimalApi.Models;

/// <summary>
/// Validator for create child tenant parameters.
/// </summary>
public sealed class CreateChildTenantParametersValidator : AbstractValidator<CreateChildTenantParameters>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateChildTenantParametersValidator"/> class.
    /// </summary>
    public CreateChildTenantParametersValidator()
    {
        this.RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        this.RuleFor(x => x.TenantName)
            .NotEmpty()
            .WithMessage("TenantName is required")
            .MaximumLength(200)
            .WithMessage("TenantName cannot exceed 200 characters")
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("TenantName can only contain alphanumeric characters, hyphens, and underscores");

        this.RuleFor(x => x.WellKnownChildTenantGuid)
            .Must(BeValidGuidOrEmpty)
            .WithMessage("WellKnownChildTenantGuid must be a valid UUID when provided");
    }

    /// <summary>
    /// Validates that a string is either empty/null or a valid GUID.
    /// </summary>
    /// <param name="guid">The string to validate.</param>
    /// <returns>True if the string is empty/null or a valid GUID; otherwise, false.</returns>
    private static bool BeValidGuidOrEmpty(string? guid) =>
        string.IsNullOrEmpty(guid) || Guid.TryParse(guid, out _);
}