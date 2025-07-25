// <copyright file="ContentTypeValidatorTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Tests.Unit.Validation;

using Marain.Tenancy.MinimalApi.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

/// <summary>
/// Unit tests for <see cref="ContentTypeValidator"/>.
/// </summary>
[TestClass]
public sealed class ContentTypeValidatorTests
{
    /// <summary>
    /// Test that valid content type for POST is accepted.
    /// </summary>
    [TestMethod]
    public void ValidateContentType_WithValidPostContentType_ShouldReturnValid()
    {
        // Act
        var result = ContentTypeValidator.ValidateContentType("POST", "application/json");

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Test that valid content type for PATCH is accepted.
    /// </summary>
    [TestMethod]
    public void ValidateContentType_WithValidPatchContentType_ShouldReturnValid()
    {
        // Act
        var result = ContentTypeValidator.ValidateContentType("PATCH", "application/json-patch+json");

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Test that content type with charset parameter is accepted.
    /// </summary>
    [TestMethod]
    public void ValidateContentType_WithContentTypeAndCharset_ShouldReturnValid()
    {
        // Act
        var result = ContentTypeValidator.ValidateContentType("POST", "application/json; charset=utf-8");

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Test that invalid content type for POST is rejected.
    /// </summary>
    [TestMethod]
    public void ValidateContentType_WithInvalidPostContentType_ShouldReturnInvalid()
    {
        // Act
        var result = ContentTypeValidator.ValidateContentType("POST", "text/plain");

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Invalid Content-Type 'text/plain' for POST");
    }

    /// <summary>
    /// Test that invalid content type for PATCH is rejected.
    /// </summary>
    [TestMethod]
    public void ValidateContentType_WithInvalidPatchContentType_ShouldReturnInvalid()
    {
        // Act
        var result = ContentTypeValidator.ValidateContentType("PATCH", "application/json");

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Invalid Content-Type 'application/json' for PATCH");
    }

    /// <summary>
    /// Test that missing content type for POST is rejected.
    /// </summary>
    [TestMethod]
    public void ValidateContentType_WithMissingContentTypeForPost_ShouldReturnInvalid()
    {
        // Act
        var result = ContentTypeValidator.ValidateContentType("POST", null);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Content-Type header is required for POST requests");
    }

    /// <summary>
    /// Test that GET requests don't require content type validation.
    /// </summary>
    [TestMethod]
    public void ValidateContentType_WithGetRequest_ShouldReturnValid()
    {
        // Act
        var result = ContentTypeValidator.ValidateContentType("GET", null);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Test that getting valid content types for POST returns expected types.
    /// </summary>
    [TestMethod]
    public void GetValidContentTypes_ForPost_ShouldReturnExpectedTypes()
    {
        // Act
        var validTypes = ContentTypeValidator.GetValidContentTypes("POST");

        // Assert
        validTypes.ShouldContain("application/json");
        validTypes.Count.ShouldBe(1);
    }

    /// <summary>
    /// Test that getting valid content types for PATCH returns expected types.
    /// </summary>
    [TestMethod]
    public void GetValidContentTypes_ForPatch_ShouldReturnExpectedTypes()
    {
        // Act
        var validTypes = ContentTypeValidator.GetValidContentTypes("PATCH");

        // Assert
        validTypes.ShouldContain("application/json-patch+json");
        validTypes.Count.ShouldBe(1);
    }

    /// <summary>
    /// Test that getting valid content types for GET returns empty collection.
    /// </summary>
    [TestMethod]
    public void GetValidContentTypes_ForGet_ShouldReturnEmpty()
    {
        // Act
        var validTypes = ContentTypeValidator.GetValidContentTypes("GET");

        // Assert
        validTypes.ShouldBeEmpty();
    }
}