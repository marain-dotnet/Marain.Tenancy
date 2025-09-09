@perFeatureContainer
@useValidation

Feature: DeleteChildTenantParametersValidation
	In order to ensure data integrity
	As a developer using the Tenancy API
	I want the DeleteChildTenantParameters to be properly validated

Background:
	Given I have a DeleteChildTenantParameters validator

Scenario: Valid parameters with all required fields
	Given I have DeleteChildTenantParameters with:
		| Field         | Value                            |
		| TenantId      | f26450ab1668784bb327951c8b08f347 |
		| ChildTenantId | 550e8400-e29b-41d4-a716-446655440000 |
	When I validate the parameters
	Then the validation should succeed

Scenario: Valid parameters with different tenant ID formats
	Given I have DeleteChildTenantParameters with:
		| Field         | Value                                |
		| TenantId      | parent-tenant-123                    |
		| ChildTenantId | child-tenant-456                     |
	When I validate the parameters
	Then the validation should succeed

Scenario Outline: Invalid TenantId values
	Given I have DeleteChildTenantParameters with:
		| Field         | Value                                |
		| TenantId      | <TenantId>                           |
		| ChildTenantId | 550e8400-e29b-41d4-a716-446655440000 |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "TenantId is required"

Examples:
	| TenantId |
	|          |
	| null     |

Scenario Outline: Invalid ChildTenantId values
	Given I have DeleteChildTenantParameters with:
		| Field         | Value                            |
		| TenantId      | f26450ab1668784bb327951c8b08f347 |
		| ChildTenantId | <ChildTenantId>                  |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "ChildTenantId is required"

Examples:
	| ChildTenantId |
	|               |
	| null          |

Scenario: Both TenantId and ChildTenantId are invalid
	Given I have DeleteChildTenantParameters with:
		| Field         | Value |
		| TenantId      |       |
		| ChildTenantId |       |
	When I validate the parameters
	Then the validation should fail
	And the validation should have multiple errors
	And the validation error should contain "TenantId is required"
	And the validation error should contain "ChildTenantId is required"