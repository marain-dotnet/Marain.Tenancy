// <copyright file="DeleteChildTenantParametersValidator.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Validation;

using FluentValidation;
using Marain.Tenancy.Api.Models;

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
        this.RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        this.RuleFor(x => x.ChildTenantId)
            .NotEmpty()
            .WithMessage("ChildTenantId is required");
    }
}