@perFeatureContainer
@useValidation

Feature: CreateChildTenantParametersValidation
	In order to ensure data integrity
	As a developer using the Tenancy API
	I want the CreateChildTenantParameters to be properly validated

Background:
	Given I have a CreateChildTenantParameters validator

Scenario: Valid parameters with all required fields
	Given I have CreateChildTenantParameters with:
		| Field      | Value                            |
		| TenantId   | f26450ab1668784bb327951c8b08f347 |
		| TenantName | ValidTenantName                  |
	When I validate the parameters
	Then the validation should succeed

Scenario: Valid parameters with well known GUID
	Given I have CreateChildTenantParameters with:
		| Field                    | Value                                |
		| TenantId                 | f26450ab1668784bb327951c8b08f347     |
		| TenantName               | ValidTenantName                      |
		| WellKnownChildTenantGuid | 550e8400-e29b-41d4-a716-446655440000 |
	When I validate the parameters
	Then the validation should succeed

Scenario Outline: Valid tenant names with allowed characters
	Given I have CreateChildTenantParameters with:
		| Field      | Value                            |
		| TenantId   | f26450ab1668784bb327951c8b08f347 |
		| TenantName | <TenantName>                     |
	When I validate the parameters
	Then the validation should succeed

Examples:
	| TenantName           |
	| Test123              |
	| Test_Name            |
	| Test-Name            |
	| Test Name            |
	| TestWithNumbers123   |
	| A                    |
	| Test_123-Name_456    |

Scenario: Valid tenant name at maximum length (200 characters)
	Given I have CreateChildTenantParameters with:
		| Field      | Value                            |
		| TenantId   | f26450ab1668784bb327951c8b08f347 |
		| TenantName | {200_character_string}           |
	When I validate the parameters
	Then the validation should succeed

Scenario Outline: Invalid TenantId values
	Given I have CreateChildTenantParameters with:
		| Field      | Value        |
		| TenantId   | <TenantId>   |
		| TenantName | ValidName    |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "TenantId is required"

Examples:
	| TenantId |
	|          |
	| null     |

Scenario: Missing request body
	Given I have CreateChildTenantParameters with TenantId "f26450ab1668784bb327951c8b08f347"
	And the request body is null
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "Request body is required"

Scenario Outline: Invalid TenantName values
	Given I have CreateChildTenantParameters with:
		| Field      | Value                            |
		| TenantId   | f26450ab1668784bb327951c8b08f347 |
		| TenantName | <TenantName>                     |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "<ExpectedError>"

Examples:
	| TenantName | ExpectedError                                                        |
	|            | TenantName is required                                               |
	| null       | TenantName is required                                               |
	| Test@Name  | TenantName can only contain alphanumeric characters, hyphens, and underscores |
	| Test#Name  | TenantName can only contain alphanumeric characters, hyphens, and underscores |
	| Test$Name  | TenantName can only contain alphanumeric characters, hyphens, and underscores |
	| Test%Name  | TenantName can only contain alphanumeric characters, hyphens, and underscores |
	| Test!Name  | TenantName can only contain alphanumeric characters, hyphens, and underscores |
	| Test.Name  | TenantName can only contain alphanumeric characters, hyphens, and underscores |
	| Test/Name  | TenantName can only contain alphanumeric characters, hyphens, and underscores |

Scenario: TenantName exceeds maximum length
	Given I have CreateChildTenantParameters with:
		| Field      | Value                            |
		| TenantId   | f26450ab1668784bb327951c8b08f347 |
		| TenantName | {201_character_string}           |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "TenantName cannot exceed 200 characters"

Scenario Outline: Invalid WellKnownChildTenantGuid values
	Given I have CreateChildTenantParameters with:
		| Field                    | Value                            |
		| TenantId                 | f26450ab1668784bb327951c8b08f347 |
		| TenantName               | ValidName                        |
		| WellKnownChildTenantGuid | <Guid>                           |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "WellKnownChildTenantGuid must be a valid UUID when provided"

Examples:
	| Guid                  |
	| invalid-guid          |
	| 123                   |
	| not-a-guid-at-all     |
	| 550e8400-e29b-41d4    |
	| 550e8400-e29b-41d4-a716-446655440000-extra |

Scenario: Empty WellKnownChildTenantGuid should be valid
	Given I have CreateChildTenantParameters with:
		| Field                    | Value                            |
		| TenantId                 | f26450ab1668784bb327951c8b08f347 |
		| TenantName               | ValidName                        |
		| WellKnownChildTenantGuid |                                  |
	When I validate the parameters
	Then the validation should succeed