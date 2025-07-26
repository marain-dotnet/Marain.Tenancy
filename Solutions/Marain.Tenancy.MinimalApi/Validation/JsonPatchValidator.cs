// <copyright file="JsonPatchValidator.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Validation;

using System.Collections.Frozen;
using System.Text.Json;

/// <summary>
/// Validates JSON Patch operations against the Tenancy API schema.
/// </summary>
public static class JsonPatchValidator
{
    /// <summary>
    /// Valid JSON Patch operations for tenant updates.
    /// </summary>
    private static readonly FrozenSet<string> ValidOperations = new[] { "add", "remove", "replace", }.ToFrozenSet();

    /// <summary>
    /// Valid paths for JSON Patch operations on tenant objects.
    /// </summary>
    private static readonly FrozenSet<string> ValidPaths = new[]
    {
        "/name",
        "/properties",
        "/properties/displayName",
        "/properties/description",
    }.ToFrozenSet();

    /// <summary>
    /// Validates a JSON Patch document for tenant operations.
    /// </summary>
    /// <param name="jsonPatchDocument">The JSON patch document to validate.</param>
    /// <returns>A validation result indicating success or failure with error details.</returns>
    public static ValidationResult ValidateJsonPatch(string jsonPatchDocument)
    {
        if (string.IsNullOrWhiteSpace(jsonPatchDocument))
        {
            return ValidationResult.Invalid("JSON Patch document cannot be empty");
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(jsonPatchDocument);

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return ValidationResult.Invalid("JSON Patch document must be an array of operations");
            }

            foreach (JsonElement operation in document.RootElement.EnumerateArray())
            {
                ValidationResult validationResult = ValidateOperation(operation);
                if (!validationResult.IsValid)
                {
                    return validationResult;
                }
            }

            return ValidationResult.Valid();
        }
        catch (JsonException ex)
        {
            return ValidationResult.Invalid($"Invalid JSON format: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a single JSON Patch operation.
    /// </summary>
    /// <param name="operation">The operation to validate.</param>
    /// <returns>A validation result for the operation.</returns>
    private static ValidationResult ValidateOperation(JsonElement operation)
    {
        if (operation.ValueKind != JsonValueKind.Object)
        {
            return ValidationResult.Invalid("Each operation must be an object");
        }

        if (!operation.TryGetProperty("op", out JsonElement opElement))
        {
            return ValidationResult.Invalid("Operation must have an 'op' property");
        }

        string? opValue = opElement.GetString();
        if (opValue is null || !ValidOperations.Contains(opValue))
        {
            return ValidationResult.Invalid($"Invalid operation: {opValue}. Valid operations are: {string.Join(", ", ValidOperations)}");
        }

        if (!operation.TryGetProperty("path", out JsonElement pathElement))
        {
            return ValidationResult.Invalid("Operation must have a 'path' property");
        }

        string? pathValue = pathElement.GetString();
        if (pathValue is null || !ValidPaths.Contains(pathValue))
        {
            return ValidationResult.Invalid($"Invalid path: {pathValue}. Valid paths are: {string.Join(", ", ValidPaths)}");
        }

        if (opValue is "add" or "replace")
        {
            if (!operation.TryGetProperty("value", out _))
            {
                return ValidationResult.Invalid($"Operation '{opValue}' requires a 'value' property");
            }
        }

        return ValidationResult.Valid();
    }
}