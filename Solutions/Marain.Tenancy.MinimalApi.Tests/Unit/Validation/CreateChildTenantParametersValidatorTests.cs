// <copyright file="CreateChildTenantParametersValidatorTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Tests.Unit.Validation;

using System;
using System.Linq;
using FluentValidation;
using Marain.Tenancy.MinimalApi.Models;
using Marain.Tenancy.MinimalApi.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

/// <summary>
/// Unit tests for <see cref="CreateChildTenantParametersValidator"/>.
/// </summary>
[TestClass]
public sealed class CreateChildTenantParametersValidatorTests
{
    private readonly CreateChildTenantParametersValidator validator = new();

    /// <summary>
    /// Test that valid parameters pass validation.
    /// </summary>
    [TestMethod]
    public void Validate_WithValidParameters_ShouldPass()
    {
        // Arrange
        var parameters = new CreateChildTenantParameters
        {
            TenantId = "parent-tenant-id",
            TenantName = "ValidTenantName"
        };

        // Act
        var result = this.validator.Validate(parameters);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// Test that valid parameters with well-known GUID pass validation.
    /// </summary>
    [TestMethod]
    public void Validate_WithValidParametersAndGuid_ShouldPass()
    {
        // Arrange
        var parameters = new CreateChildTenantParameters
        {
            TenantId = "parent-tenant-id",
            TenantName = "ValidTenantName",
            WellKnownChildTenantGuid = Guid.NewGuid().ToString()
        };

        // Act
        var result = this.validator.Validate(parameters);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// Test that empty tenant ID fails validation.
    /// </summary>
    [TestMethod]
    public void Validate_WithEmptyTenantId_ShouldFail()
    {
        // Arrange
        var parameters = new CreateChildTenantParameters
        {
            TenantId = "",
            TenantName = "ValidTenantName"
        };

        // Act
        var result = this.validator.Validate(parameters);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateChildTenantParameters.TenantId) && e.ErrorMessage == "TenantId is required");
    }

    /// <summary>
    /// Test that empty tenant name fails validation.
    /// </summary>
    [TestMethod]
    public void Validate_WithEmptyTenantName_ShouldFail()
    {
        // Arrange
        var parameters = new CreateChildTenantParameters
        {
            TenantId = "parent-tenant-id",
            TenantName = ""
        };

        // Act
        var result = this.validator.Validate(parameters);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateChildTenantParameters.TenantName) && e.ErrorMessage == "TenantName is required");
    }

    /// <summary>
    /// Test that tenant name exceeding maximum length fails validation.
    /// </summary>
    [TestMethod]
    public void Validate_WithTenantNameTooLong_ShouldFail()
    {
        // Arrange
        var longName = new string('a', 201); // Exceeds 200 character limit
        var parameters = new CreateChildTenantParameters
        {
            TenantId = "parent-tenant-id",
            TenantName = longName
        };

        // Act
        var result = this.validator.Validate(parameters);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateChildTenantParameters.TenantName) && e.ErrorMessage == "TenantName cannot exceed 200 characters");
    }

    /// <summary>
    /// Test that tenant name with invalid characters fails validation.
    /// </summary>
    [TestMethod]
    public void Validate_WithInvalidCharactersInTenantName_ShouldFail()
    {
        // Arrange
        var parameters = new CreateChildTenantParameters
        {
            TenantId = "parent-tenant-id",
            TenantName = "Invalid Name With Spaces!"
        };

        // Act
        var result = this.validator.Validate(parameters);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateChildTenantParameters.TenantName) && e.ErrorMessage == "TenantName can only contain alphanumeric characters, hyphens, and underscores");
    }

    /// <summary>
    /// Test that valid tenant name with allowed characters passes validation.
    /// </summary>
    [TestMethod]
    public void Validate_WithValidTenantNameCharacters_ShouldPass()
    {
        // Arrange
        var parameters = new CreateChildTenantParameters
        {
            TenantId = "parent-tenant-id",
            TenantName = "Valid-Tenant_Name123"
        };

        // Act
        var result = this.validator.Validate(parameters);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.Where(e => e.PropertyName == nameof(CreateChildTenantParameters.TenantName)).ShouldBeEmpty();
    }

    /// <summary>
    /// Test that invalid GUID format fails validation.
    /// </summary>
    [TestMethod]
    public void Validate_WithInvalidGuidFormat_ShouldFail()
    {
        // Arrange
        var parameters = new CreateChildTenantParameters
        {
            TenantId = "parent-tenant-id",
            TenantName = "ValidTenantName",
            WellKnownChildTenantGuid = "not-a-valid-guid"
        };

        // Act
        var result = this.validator.Validate(parameters);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateChildTenantParameters.WellKnownChildTenantGuid) && e.ErrorMessage == "WellKnownChildTenantGuid must be a valid UUID when provided");
    }

    /// <summary>
    /// Test that null or empty GUID is allowed.
    /// </summary>
    [TestMethod]
    public void Validate_WithNullOrEmptyGuid_ShouldPass()
    {
        // Arrange
        var parametersWithNull = new CreateChildTenantParameters
        {
            TenantId = "parent-tenant-id",
            TenantName = "ValidTenantName",
            WellKnownChildTenantGuid = null
        };

        var parametersWithEmpty = new CreateChildTenantParameters
        {
            TenantId = "parent-tenant-id",
            TenantName = "ValidTenantName",
            WellKnownChildTenantGuid = ""
        };

        // Act
        var resultNull = this.validator.Validate(parametersWithNull);
        var resultEmpty = this.validator.Validate(parametersWithEmpty);

        // Assert
        resultNull.IsValid.ShouldBeTrue();
        resultNull.Errors.Where(e => e.PropertyName == nameof(CreateChildTenantParameters.WellKnownChildTenantGuid)).ShouldBeEmpty();
        resultEmpty.IsValid.ShouldBeTrue();
        resultEmpty.Errors.Where(e => e.PropertyName == nameof(CreateChildTenantParameters.WellKnownChildTenantGuid)).ShouldBeEmpty();
    }
}