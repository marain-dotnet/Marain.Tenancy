// <copyright file="JsonPatchValidatorTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Tests.Unit.Validation;

using Marain.Tenancy.MinimalApi.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

/// <summary>
/// Unit tests for <see cref="JsonPatchValidator"/>.
/// </summary>
[TestClass]
public sealed class JsonPatchValidatorTests
{
    /// <summary>
    /// Test that valid JSON Patch operations are accepted.
    /// </summary>
    [TestMethod]
    public void ValidateJsonPatch_WithValidOperations_ShouldReturnValid()
    {
        // Arrange
        var validJsonPatch = """
            [
                { "op": "replace", "path": "/name", "value": "Updated Name" },
                { "op": "add", "path": "/properties/description", "value": "New description" },
                { "op": "remove", "path": "/properties/oldProperty" }
            ]
            """;

        // Act
        var result = JsonPatchValidator.ValidateJsonPatch(validJsonPatch);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    /// <summary>
    /// Test that empty JSON Patch document is rejected.
    /// </summary>
    [TestMethod]
    public void ValidateJsonPatch_WithEmptyDocument_ShouldReturnInvalid()
    {
        // Act
        var result = JsonPatchValidator.ValidateJsonPatch("");

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("JSON Patch document cannot be empty");
    }

    /// <summary>
    /// Test that invalid JSON is rejected.
    /// </summary>
    [TestMethod]
    public void ValidateJsonPatch_WithInvalidJson_ShouldReturnInvalid()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var result = JsonPatchValidator.ValidateJsonPatch(invalidJson);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldStartWith("Invalid JSON format:");
    }

    /// <summary>
    /// Test that non-array JSON is rejected.
    /// </summary>
    [TestMethod]
    public void ValidateJsonPatch_WithNonArrayJson_ShouldReturnInvalid()
    {
        // Arrange
        var nonArrayJson = """{ "op": "replace", "path": "/name", "value": "test" }""";

        // Act
        var result = JsonPatchValidator.ValidateJsonPatch(nonArrayJson);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("JSON Patch document must be an array of operations");
    }

    /// <summary>
    /// Test that invalid operation is rejected.
    /// </summary>
    [TestMethod]
    public void ValidateJsonPatch_WithInvalidOperation_ShouldReturnInvalid()
    {
        // Arrange
        var invalidOperation = """[{ "op": "invalid", "path": "/name", "value": "test" }]""";

        // Act
        var result = JsonPatchValidator.ValidateJsonPatch(invalidOperation);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Invalid operation: invalid");
    }

    /// <summary>
    /// Test that invalid path is rejected.
    /// </summary>
    [TestMethod]
    public void ValidateJsonPatch_WithInvalidPath_ShouldReturnInvalid()
    {
        // Arrange
        var invalidPath = """[{ "op": "replace", "path": "/invalidPath", "value": "test" }]""";

        // Act
        var result = JsonPatchValidator.ValidateJsonPatch(invalidPath);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Invalid path: /invalidPath");
    }

    /// <summary>
    /// Test that add operation without value is rejected.
    /// </summary>
    [TestMethod]
    public void ValidateJsonPatch_WithAddOperationWithoutValue_ShouldReturnInvalid()
    {
        // Arrange
        var missingValue = """[{ "op": "add", "path": "/name" }]""";

        // Act
        var result = JsonPatchValidator.ValidateJsonPatch(missingValue);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Operation 'add' requires a 'value' property");
    }

    /// <summary>
    /// Test that replace operation without value is rejected.
    /// </summary>
    [TestMethod]
    public void ValidateJsonPatch_WithReplaceOperationWithoutValue_ShouldReturnInvalid()
    {
        // Arrange
        var missingValue = """[{ "op": "replace", "path": "/name" }]""";

        // Act
        var result = JsonPatchValidator.ValidateJsonPatch(missingValue);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Operation 'replace' requires a 'value' property");
    }

    /// <summary>
    /// Test that remove operation without value is accepted.
    /// </summary>
    [TestMethod]
    public void ValidateJsonPatch_WithRemoveOperationWithoutValue_ShouldReturnValid()
    {
        // Arrange
        var removeOperation = """[{ "op": "remove", "path": "/properties" }]""";

        // Act
        var result = JsonPatchValidator.ValidateJsonPatch(removeOperation);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}