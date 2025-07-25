// <copyright file="TenantParametersValidator.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Validation;

using FluentValidation;
using Marain.Tenancy.MinimalApi.Models;

/// <summary>
/// Validator for tenant operation parameters.
/// </summary>
public class TenantParametersValidator : AbstractValidator<TenantParameters>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantParametersValidator"/> class.
    /// </summary>
    public TenantParametersValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        RuleFor(x => x.TenantName)
            .NotEmpty()
            .When(x => x.IsCreateChildTenantRequest)
            .WithMessage("TenantName is required for child tenant creation");

        RuleFor(x => x.WellKnownChildTenantGuid)
            .Must(BeValidGuid)
            .When(x => !string.IsNullOrEmpty(x.WellKnownChildTenantGuid))
            .WithMessage("WellKnownChildTenantGuid must be a valid UUID");

        RuleFor(x => x.MaxItems)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .When(x => x.MaxItems.HasValue)
            .WithMessage("MaxItems must be between 1 and 1000");

        RuleFor(x => x.ChildTenantId)
            .NotEmpty()
            .When(x => x.IsDeleteChildTenantRequest)
            .WithMessage("ChildTenantId is required for delete operations");
    }

    /// <summary>
    /// Determines whether the specified string is a valid GUID.
    /// </summary>
    /// <param name="guid">The string to validate.</param>
    /// <returns>True if the string is a valid GUID; otherwise, false.</returns>
    private static bool BeValidGuid(string? guid) => Guid.TryParse(guid, out _);
}