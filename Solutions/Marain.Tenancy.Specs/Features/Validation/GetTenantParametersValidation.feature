@perFeatureContainer
@useValidation

Feature: GetTenantParametersValidation
	In order to ensure data integrity
	As a developer using the Tenancy API
	I want the GetTenantParameters to be properly validated

Background:
	Given I have a GetTenantParameters validator

Scenario: Valid parameters with required field
	Given I have GetTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	When I validate the parameters
	Then the validation should succeed

Scenario: Valid parameters with different tenant ID formats
	Given I have GetTenantParameters with:
		| Field    | Value                                |
		| TenantId | tenant-123-abc                       |
	When I validate the parameters
	Then the validation should succeed

Scenario: Valid parameters with GUID format tenant ID
	Given I have GetTenantParameters with:
		| Field    | Value                                |
		| TenantId | 550e8400-e29b-41d4-a716-446655440000 |
	When I validate the parameters
	Then the validation should succeed

Scenario: Valid parameters with root tenant ID
	Given I have GetTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	When I validate the parameters
	Then the validation should succeed

Scenario Outline: Invalid TenantId values
	Given I have GetTenantParameters with:
		| Field    | Value      |
		| TenantId | <TenantId> |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "TenantId is required"

Examples:
	| TenantId |
	|          |
	| null     |