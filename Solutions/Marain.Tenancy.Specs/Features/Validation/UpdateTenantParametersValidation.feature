@perFeatureContainer
@useValidation

Feature: UpdateTenantParametersValidation
	In order to ensure data integrity
	As a developer using the Tenancy API
	I want the UpdateTenantParameters to be properly validated

Background:
	Given I have an UpdateTenantParameters validator

Scenario: Valid parameters with single patch entry
	Given I have UpdateTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	And I have patch entries:
		| Operation | Path  | Value   |
		| Replace   | /name | NewName |
	When I validate the parameters
	Then the validation should succeed

Scenario: Valid parameters with multiple patch entries
	Given I have UpdateTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	And I have patch entries:
		| Operation | Path                 | Value       |
		| Replace   | /name                | UpdatedName |
		| Add       | /properties/setting1 | value123    |
		| Remove    | /properties/setting2 | null        |
	When I validate the parameters
	Then the validation should succeed

Scenario: Valid parameters with property operations
	Given I have UpdateTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	And I have patch entries:
		| Operation | Path                     | Value        |
		| Add       | /properties/newProperty  | newValue     |
		| Replace   | /properties/existingProp | updatedValue |
	When I validate the parameters
	Then the validation should succeed

Scenario Outline: Invalid TenantId values
	Given I have UpdateTenantParameters with:
		| Field    | Value      |
		| TenantId | <TenantId> |
	And I have patch entries:
		| Operation | Path  | Value   |
		| Replace   | /name | NewName |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "TenantId is required"

Examples:
	| TenantId |
	|          |
	| null     |

Scenario: Missing patch array
	Given I have UpdateTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	And the patch array is null
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "Body is required"

Scenario: Empty patch array
	Given I have UpdateTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	And I have an empty patch entries array
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "Body is required"

Scenario: Invalid patch entry in array
	Given I have UpdateTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	And I have patch entries:
		| Operation | Path     | Value   |
		| Replace   | /invalid | NewName |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "Path is required and must either be "/name" or start with "/properties/""

Scenario: Multiple invalid patch entries
	Given I have UpdateTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	And I have patch entries:
		| Operation | Path     | Value   |
		| Add       | /invalid | null    |
		| Replace   | /bad     | null    |
	When I validate the parameters
	Then the validation should fail
	And the validation should have multiple errors
	And the validation error should contain "Path is required and must either be "/name" or start with "/properties/""
	And the validation error should contain "When Op is not 'remove', Value must be provided."

Scenario: Mix of valid and invalid patch entries
	Given I have UpdateTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	And I have patch entries:
		| Operation | Path                | Value       |
		| Replace   | /name               | ValidName   |
		| Add       | /invalid            | InvalidPath |
		| Remove    | /properties/setting | null        |
	When I validate the parameters
	Then the validation should fail
	And the validation error should contain "Path is required and must either be "/name" or start with "/properties/""

Scenario: Null entry in patch array
	Given I have UpdateTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	And I have a patch array with null entries
	When I validate the parameters
	Then the validation should fail

Scenario: Multiple validation errors at top level
	Given I have UpdateTenantParameters with:
		| Field    | Value |
		| TenantId |       |
	And the patch array is null
	When I validate the parameters
	Then the validation should fail
	And the validation should have multiple errors
	And the validation error should contain "TenantId is required"
	And the validation error should contain "Body is required"

Scenario: Valid complex patch operations
	Given I have UpdateTenantParameters with:
		| Field    | Value                            |
		| TenantId | f26450ab1668784bb327951c8b08f347 |
	And I have patch entries:
		| Operation | Path                          | Value                    |
		| Replace   | /name                         | ComplexTenantName        |
		| Add       | /properties/config            | {"enabled": true}        |
		| Add       | /properties/tags              | ["tag1", "tag2"]         |
		| Remove    | /properties/oldSetting        | null                     |
		| Replace   | /properties/existingSetting   | updatedComplexValue      |
	When I validate the parameters
	Then the validation should succeed