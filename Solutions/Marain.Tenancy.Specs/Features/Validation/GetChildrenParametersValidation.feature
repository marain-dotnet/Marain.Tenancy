@perFeatureContainer
@useValidation

Feature: GetChildrenParametersValidation
	In order to ensure data integrity
	As a developer using the Tenancy API
	I want the GetChildrenParameters to be properly validated

Background:
	Given I have a GetChildrenParameters validator

Scenario: Valid parameters with only required field
	Given I have GetChildrenParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	When I validate the parameters
	Then the validation should succeed

Scenario: Valid parameters with MaxItems
	Given I have GetChildrenParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
		| MaxItems | 50                               |
	When I validate the parameters
	Then the validation should succeed

Scenario: Valid parameters with ContinuationToken
	Given I have GetChildrenParameters with:
		| Field             | Value                            |
		| TenantId          | f26450ab1668784bb327951c8b08f347 |
		| ContinuationToken | someValidToken123                |
	When I validate the parameters
	Then the validation should succeed

Scenario: Valid parameters with all optional fields
	Given I have GetChildrenParameters with:
		| Field             | Value                            |
		| TenantId          | f26450ab1668784bb327951c8b08f347 |
		| MaxItems          | 100                              |
		| ContinuationToken | someValidToken123                |
	When I validate the parameters
	Then the validation should succeed

Scenario Outline: Valid MaxItems boundary values
	Given I have GetChildrenParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
		| MaxItems | <MaxItems>                       |
	When I validate the parameters
	Then the validation should succeed

Examples:
	| MaxItems |
	| 1        |
	| 100      |
	| 500      |
	| 1000     |

Scenario: Valid ContinuationToken at maximum length (500 characters)
	Given I have GetChildrenParameters with:
		| Field             | Value                            |
		| TenantId          | f26450ab1668784bb327951c8b08f347 |
		| ContinuationToken | {500_character_string}           |
	When I validate the parameters
	Then the validation should succeed

Scenario Outline: Invalid TenantId values
	Given I have GetChildrenParameters with:
		| Field    | Value      |
		| TenantId | <TenantId> |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "TenantId is required"

Examples:
	| TenantId |
	|          |
	| null     |

Scenario Outline: Invalid MaxItems values (over 0)
	Given I have GetChildrenParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
		| MaxItems | <MaxItems>                       |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "MaxItems must be between 1 and 1000"

Examples:
	| MaxItems |
	| 1001     |
	| 2000     |
	| 999999   |

Scenario Outline: Invalid MaxItems values (under 0)
	Given I have GetChildrenParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
		| MaxItems | <MaxItems>                       |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "'Max Items' must be greater than '0'."

Examples:
	| MaxItems |
	| 0        |
	| -1       |
	| -100     |

Scenario: ContinuationToken exceeds maximum length
	Given I have GetChildrenParameters with:
		| Field             | Value                            |
		| TenantId          | f26450ab1668784bb327951c8b08f347 |
		| ContinuationToken | {501_character_string}           |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "ContinuationToken cannot exceed 500 characters"

Scenario: Null MaxItems should be valid
	Given I have GetChildrenParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
		| MaxItems | null                             |
	When I validate the parameters
	Then the validation should succeed

Scenario: Empty ContinuationToken should be valid
	Given I have GetChildrenParameters with:
		| Field             | Value                            |
		| TenantId          | f26450ab1668784bb327951c8b08f347 |
		| ContinuationToken |                                  |
	When I validate the parameters
	Then the validation should succeed

Scenario: Null ContinuationToken should be valid
	Given I have GetChildrenParameters with:
		| Field             | Value                            |
		| TenantId          | f26450ab1668784bb327951c8b08f347 |
		| ContinuationToken | null                             |
	When I validate the parameters
	Then the validation should succeed

Scenario: Multiple validation errors
	Given I have GetChildrenParameters with:
		| Field             | Value                  |
		| TenantId          |                        |
		| MaxItems          | 0                      |
		| ContinuationToken | {501_character_string} |
	When I validate the parameters
	Then the validation should fail
	And the validation should have multiple errors
	And the validation error should contain "TenantId is required"
	And the validation error should contain "'Max Items' must be greater than '0'."
	And the validation error should contain "ContinuationToken cannot exceed 500 characters"