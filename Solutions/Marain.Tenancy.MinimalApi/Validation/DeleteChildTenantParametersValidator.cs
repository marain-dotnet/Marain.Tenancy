// <copyright file="DeleteChildTenantParametersValidator.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Validation;

using FluentValidation;
using Marain.Tenancy.MinimalApi.Models;

/// <summary>
/// Validator for delete child tenant parameters.
/// </summary>
public sealed class DeleteChildTenantParametersValidator : AbstractValidator<DeleteChildTenantParameters>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteChildTenantParametersValidator"/> class.
    /// </summary>
    public DeleteChildTenantParametersValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        RuleFor(x => x.ChildTenantId)
            .NotEmpty()
            .WithMessage("ChildTenantId is required");
    }
}