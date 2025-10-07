// <copyright file="GetChildrenParametersValidator.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Validation;

using FluentValidation;
using Marain.Tenancy.Api.Models;

/// <summary>
/// Validator for get children parameters.
/// </summary>
public sealed class GetChildrenParametersValidator : AbstractValidator<GetChildrenParameters>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetChildrenParametersValidator"/> class.
    /// </summary>
    public GetChildrenParametersValidator()
    {
        this.RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        this.RuleFor(x => x.MaxItems)
            .GreaterThan(0)
            .LessThanOrEqualTo(1000)
            .When(x => x.MaxItems.HasValue)
            .WithMessage("MaxItems must be between 1 and 1000");

        this.RuleFor(x => x.ContinuationToken)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.ContinuationToken))
            .WithMessage("ContinuationToken cannot exceed 500 characters");
    }
}