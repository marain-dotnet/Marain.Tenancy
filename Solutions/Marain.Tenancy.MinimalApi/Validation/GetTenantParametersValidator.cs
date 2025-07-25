// <copyright file="GetTenantParametersValidator.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Validation;

using FluentValidation;
using Marain.Tenancy.MinimalApi.Models;

/// <summary>
/// Validator for get tenant parameters.
/// </summary>
public sealed class GetTenantParametersValidator : AbstractValidator<GetTenantParameters>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetTenantParametersValidator"/> class.
    /// </summary>
    public GetTenantParametersValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");
    }
}