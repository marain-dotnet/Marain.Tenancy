// <copyright file="ValidationSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Steps;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Marain.Tenancy.Api.Models;
using Marain.Tenancy.Api.Validation;
using NUnit.Framework;
using Reqnroll;

/// <summary>
/// Step definitions for validation feature tests.
/// </summary>
[Binding]
public class ValidationSteps
{
    private const string ValidationResultKey = "ValidationResult";
    private const string ValidatorKey = "Validator";
    private const string ParametersKey = "Parameters";

    private readonly ScenarioContext scenarioContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationSteps"/> class.
    /// </summary>
    /// <param name="scenarioContext">The scenario context.</param>
    public ValidationSteps(ScenarioContext scenarioContext)
    {
        this.scenarioContext = scenarioContext;
    }

    [Given("I have a CreateChildTenantParameters validator")]
    public void GivenIHaveACreateChildTenantParametersValidator()
    {
        var validator = new CreateChildTenantParametersValidator();
        this.scenarioContext.Set(validator, ValidatorKey);
    }

    [Given("I have a DeleteChildTenantParameters validator")]
    public void GivenIHaveADeleteChildTenantParametersValidator()
    {
        var validator = new DeleteChildTenantParametersValidator();
        this.scenarioContext.Set(validator, ValidatorKey);
    }

    [Given("I have a GetChildrenParameters validator")]
    public void GivenIHaveAGetChildrenParametersValidator()
    {
        var validator = new GetChildrenParametersValidator();
        this.scenarioContext.Set(validator, ValidatorKey);
    }

    [Given("I have a GetTenantParameters validator")]
    public void GivenIHaveAGetTenantParametersValidator()
    {
        var validator = new GetTenantParametersValidator();
        this.scenarioContext.Set(validator, ValidatorKey);
    }

    [Given("I have an UpdateTenantJsonPatchEntry validator")]
    public void GivenIHaveAnUpdateTenantJsonPatchEntryValidator()
    {
        var validator = new UpdateTenantJsonPatchEntryValidator();
        this.scenarioContext.Set(validator, ValidatorKey);
    }

    [Given("I have an UpdateTenantParameters validator")]
    public void GivenIHaveAnUpdateTenantParametersValidator()
    {
        var validator = new UpdateTenantParametersValidator();
        this.scenarioContext.Set(validator, ValidatorKey);
    }

    [Given("I have CreateChildTenantParameters with:")]
    public void GivenIHaveCreateChildTenantParametersWith(Table table)
    {
        string? tenantId = null;
        string? tenantName = null;
        string? wellKnownGuid = null;

        foreach (DataTableRow row in table.Rows)
        {
            string field = row["Field"];
            string value = row["Value"];

            switch (field)
            {
                case "TenantId":
                    tenantId = ParseNullableString(value);
                    break;
                case "TenantName":
                    tenantName = ParseSpecialString(value);
                    break;
                case "WellKnownChildTenantGuid":
                    wellKnownGuid = ParseNullableString(value);
                    break;
            }
        }

        var request = new CreateChildTenantRequest
        {
            TenantName = tenantName ?? string.Empty,
            WellKnownChildTenantGuid = wellKnownGuid,
        };

        var parameters = new CreateChildTenantParameters
        {
            TenantId = tenantId ?? string.Empty,
            Request = request,
        };

        this.scenarioContext.Set(parameters, ParametersKey);
    }

    [Given("I have CreateChildTenantParameters with TenantId \"(.*)\"")]
    public void GivenIHaveCreateChildTenantParametersWithTenantId(string tenantId)
    {
        var request = new CreateChildTenantRequest
        {
            TenantName = "DefaultName",
        };

        var parameters = new CreateChildTenantParameters
        {
            TenantId = tenantId,
            Request = request,
        };

        this.scenarioContext.Set(parameters, ParametersKey);
    }

    [Given("the request body is null")]
    public void GivenTheRequestBodyIsNull()
    {
        if (this.scenarioContext.TryGetValue(ParametersKey, out CreateChildTenantParameters? parameters))
        {
            var newParameters = new CreateChildTenantParameters
            {
                TenantId = parameters!.TenantId,
                Request = null!,
            };
            this.scenarioContext.Set(newParameters, ParametersKey);
        }
    }

    [Given("I have DeleteChildTenantParameters with:")]
    public void GivenIHaveDeleteChildTenantParametersWith(Table table)
    {
        string? tenantId = null;
        string? childTenantId = null;

        foreach (DataTableRow row in table.Rows)
        {
            string field = row["Field"];
            string value = row["Value"];

            switch (field)
            {
                case "TenantId":
                    tenantId = ParseNullableString(value);
                    break;
                case "ChildTenantId":
                    childTenantId = ParseNullableString(value);
                    break;
            }
        }

        var parameters = new DeleteChildTenantParameters
        {
            TenantId = tenantId ?? string.Empty,
            ChildTenantId = childTenantId ?? string.Empty,
        };

        this.scenarioContext.Set(parameters, ParametersKey);
    }

    [Given("I have GetChildrenParameters with:")]
    public void GivenIHaveGetChildrenParametersWith(Table table)
    {
        string? tenantId = null;
        int? maxItems = null;
        string? continuationToken = null;

        foreach (DataTableRow row in table.Rows)
        {
            string field = row["Field"];
            string value = row["Value"];

            switch (field)
            {
                case "TenantId":
                    tenantId = ParseNullableString(value);
                    break;
                case "MaxItems":
                    maxItems = ParseNullableInt(value);
                    break;
                case "ContinuationToken":
                    continuationToken = ParseSpecialString(value);
                    break;
            }
        }

        var parameters = new GetChildrenParameters
        {
            TenantId = tenantId ?? string.Empty,
            MaxItems = maxItems,
            ContinuationToken = continuationToken,
        };

        this.scenarioContext.Set(parameters, ParametersKey);
    }

    [Given("I have GetTenantParameters with:")]
    public void GivenIHaveGetTenantParametersWith(Table table)
    {
        string? tenantId = null;

        foreach (DataTableRow row in table.Rows)
        {
            string field = row["Field"];
            string value = row["Value"];

            if (field == "TenantId")
            {
                tenantId = ParseNullableString(value);
            }
        }

        var parameters = new GetTenantParameters
        {
            TenantId = tenantId ?? string.Empty,
        };

        this.scenarioContext.Set(parameters, ParametersKey);
    }

    [Given("I have UpdateTenantJsonPatchEntry with:")]
    public void GivenIHaveUpdateTenantJsonPatchEntryWith(Table table)
    {
        UpdateTenantJsonPatchEntryOperation? operation = null;
        string? path = null;
        object? value = null;

        foreach (DataTableRow row in table.Rows)
        {
            string field = row["Field"];
            string valueStr = row["Value"];

            switch (field)
            {
                case "Operation":
                    operation = ParseOperation(valueStr);
                    break;
                case "Path":
                    path = ParseNullableString(valueStr);
                    break;
                case "Value":
                    value = ParseJsonValue(valueStr);
                    break;
            }
        }

        var entry = new UpdateTenantJsonPatchEntry
        {
            Operation = operation ?? UpdateTenantJsonPatchEntryOperation.Add,
            Path = path ?? string.Empty,
            Value = value,
        };

        this.scenarioContext.Set(entry, ParametersKey);
    }

    [Given("I have UpdateTenantParameters with:")]
    public void GivenIHaveUpdateTenantParametersWith(Table table)
    {
        string? tenantId = null;

        foreach (DataTableRow row in table.Rows)
        {
            string field = row["Field"];
            string value = row["Value"];

            if (field == "TenantId")
            {
                tenantId = ParseNullableString(value);
            }
        }

        var parameters = new UpdateTenantParameters
        {
            TenantId = tenantId ?? string.Empty,
            UpdateTenantJsonPatchArray = [],
        };

        this.scenarioContext.Set(parameters, ParametersKey);
    }

    [Given("I have patch entries:")]
    public void GivenIHavePatchEntries(Table table)
    {
        var entries = new List<UpdateTenantJsonPatchEntry>();

        foreach (DataTableRow row in table.Rows)
        {
            UpdateTenantJsonPatchEntryOperation? operation = ParseOperation(row["Operation"]);
            string path = row["Path"];
            object? value = ParseJsonValue(row["Value"]);

            entries.Add(new UpdateTenantJsonPatchEntry
            {
                Operation = operation ?? UpdateTenantJsonPatchEntryOperation.Add,
                Path = path,
                Value = value,
            });
        }

        if (this.scenarioContext.TryGetValue(ParametersKey, out UpdateTenantParameters? parameters))
        {
            var newParameters = new UpdateTenantParameters
            {
                TenantId = parameters!.TenantId,
                UpdateTenantJsonPatchArray = entries.ToArray(),
            };
            this.scenarioContext.Set(newParameters, ParametersKey);
        }
    }

    [Given("the patch array is null")]
    public void GivenThePatchArrayIsNull()
    {
        if (this.scenarioContext.TryGetValue(ParametersKey, out UpdateTenantParameters? parameters))
        {
            var newParameters = new UpdateTenantParameters
            {
                TenantId = parameters!.TenantId,
                UpdateTenantJsonPatchArray = null!,
            };
            this.scenarioContext.Set(newParameters, ParametersKey);
        }
    }

    [Given("I have an empty patch entries array")]
    public void GivenIHaveAnEmptyPatchEntriesArray()
    {
        if (this.scenarioContext.TryGetValue(ParametersKey, out UpdateTenantParameters? parameters))
        {
            var newParameters = new UpdateTenantParameters
            {
                TenantId = parameters!.TenantId,
                UpdateTenantJsonPatchArray = [],
            };
            this.scenarioContext.Set(newParameters, ParametersKey);
        }
    }

    [Given("I have a patch array with null entries")]
    public void GivenIHaveAPatchArrayWithNullEntries()
    {
        if (this.scenarioContext.TryGetValue(ParametersKey, out UpdateTenantParameters? parameters))
        {
            var newParameters = new UpdateTenantParameters
            {
                TenantId = parameters!.TenantId,
                UpdateTenantJsonPatchArray = [null!],
            };
            this.scenarioContext.Set(newParameters, ParametersKey);
        }
    }

    [When("I validate the parameters")]
    [When("I validate the entry")]
    public void WhenIValidateTheParameters()
    {
        IValidator validator = this.scenarioContext.Get<IValidator>(ValidatorKey);
        object parameters = this.scenarioContext.Get<object>(ParametersKey);

        ValidationResult result = validator.Validate(new ValidationContext<object>(parameters));
        this.scenarioContext.Set(result, ValidationResultKey);
    }

    [Then("the validation should succeed")]
    public void ThenTheValidationShouldSucceed()
    {
        ValidationResult result = this.scenarioContext.Get<ValidationResult>(ValidationResultKey);
        Assert.That(result.IsValid, Is.True, $"Validation should have succeeded but failed with errors: {string.Join(", ", result.Errors.Select(e => e.ErrorMessage))}");
    }

    [Then("the validation should fail")]
    public void ThenTheValidationShouldFail()
    {
        ValidationResult result = this.scenarioContext.Get<ValidationResult>(ValidationResultKey);
        Assert.That(result.IsValid, Is.False, "Validation should have failed but succeeded");
    }

    [Then("the validation error should contain \"(.*)\"")]
    public void ThenTheValidationErrorShouldContain(string expectedMessage)
    {
        ValidationResult result = this.scenarioContext.Get<ValidationResult>(ValidationResultKey);
        string errorMessages = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
        Assert.That(errorMessages, Does.Contain(expectedMessage), $"Expected error message '{expectedMessage}' not found in: {errorMessages}");
    }

    [Then("the validation should have multiple errors")]
    public void ThenTheValidationShouldHaveMultipleErrors()
    {
        ValidationResult result = this.scenarioContext.Get<ValidationResult>(ValidationResultKey);
        Assert.That(result.Errors.Count, Is.GreaterThan(1), $"Expected multiple validation errors but found {result.Errors.Count}");
    }

    private static string? ParseNullableString(string value)
    {
        return value switch
        {
            "" => string.Empty,
            "null" => null,
            _ => value,
        };
    }

    private static string? ParseSpecialString(string value)
    {
        return value switch
        {
            "" => string.Empty,
            "null" => null,
            "{200_character_string}" => new string('A', 200),
            "{201_character_string}" => new string('A', 201),
            "{500_character_string}" => new string('B', 500),
            "{501_character_string}" => new string('B', 501),
            _ => value,
        };
    }

    private static int? ParseNullableInt(string value)
    {
        return value switch
        {
            "null" => null,
            _ when int.TryParse(value, out int result) => result,
            _ => null,
        };
    }

    private static UpdateTenantJsonPatchEntryOperation? ParseOperation(string value)
    {
        return value switch
        {
            "Add" => UpdateTenantJsonPatchEntryOperation.Add,
            "Replace" => UpdateTenantJsonPatchEntryOperation.Replace,
            "Remove" => UpdateTenantJsonPatchEntryOperation.Remove,
            _ => UpdateTenantJsonPatchEntryOperation.Add, // Default to Add for any unknown value
        };
    }

    private static object? ParseJsonValue(string value)
    {
        return value switch
        {
            "null" => null,
            "" => string.Empty,
            _ when value.StartsWith("{") || value.StartsWith("[") => JsonSerializer.Deserialize<JsonElement>(value),
            _ => value,
        };
    }
}