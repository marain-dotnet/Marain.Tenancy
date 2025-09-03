// <copyright file="UpdateTenantJsonPatchEntryValidator.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Api.Validation;

using FluentValidation;
using Marain.Tenancy.Api.Models;

/// <summary>
/// Validator for update tenant parameters.
/// </summary>
public sealed class UpdateTenantJsonPatchEntryValidator : AbstractValidator<UpdateTenantJsonPatchEntry>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTenantJsonPatchEntryValidator"/> class.
    /// </summary>
    public UpdateTenantJsonPatchEntryValidator()
    {
        this.RuleFor(x => x.Operation)
            .NotNull()
            .IsInEnum()
            .WithMessage("Op is required");

        this.RuleFor(x => x.Path)
            .NotNull()
            .NotEmpty()
            .Must(x => x == "/name" || x.StartsWith("/properties/"))
            .WithMessage("Path is required and must either be \"/name\" or start with \"/properties/\"");

        this.RuleFor(x => x.Value)
            .Null()
            .When(x => x.Operation == UpdateTenantJsonPatchEntryOperation.Remove)
            .WithMessage("When Op is 'remove', Value must be left as null.");

        this.RuleFor(x => x.Value)
            .NotNull()
            .Unless(x => x.Operation == UpdateTenantJsonPatchEntryOperation.Remove)
            .WithMessage("When Op is not 'remove', Value must be provided.");
    }
}