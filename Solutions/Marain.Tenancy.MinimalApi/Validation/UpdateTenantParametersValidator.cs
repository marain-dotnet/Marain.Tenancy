// <copyright file="UpdateTenantParametersValidator.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Validation;

using FluentValidation;
using Marain.Tenancy.MinimalApi.Models;

/// <summary>
/// Validator for update tenant parameters.
/// </summary>
public sealed class UpdateTenantParametersValidator : AbstractValidator<UpdateTenantParameters>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTenantParametersValidator"/> class.
    /// </summary>
    public UpdateTenantParametersValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");
    }
}