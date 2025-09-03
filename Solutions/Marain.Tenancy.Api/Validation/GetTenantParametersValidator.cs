// <copyright file="GetTenantParametersValidator.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Validation;

using FluentValidation;
using Marain.Tenancy.Api.Models;

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
        this.RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");
    }
}