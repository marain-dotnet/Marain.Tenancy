@perFeatureContainer
@useValidation

Feature: UpdateTenantJsonPatchEntryValidation
	In order to ensure data integrity
	As a developer using the Tenancy API
	I want the UpdateTenantJsonPatchEntry to be properly validated

Background:
	Given I have an UpdateTenantJsonPatchEntry validator

Scenario: Valid Add operation for name property
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value     |
		| Operation | Add       |
		| Path      | /name     |
		| Value     | NewName   |
	When I validate the entry
	Then the validation should succeed

Scenario: Valid Add operation for properties
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value                |
		| Operation | Add                  |
		| Path      | /properties/setting1 |
		| Value     | value123             |
	When I validate the entry
	Then the validation should succeed

Scenario: Valid Replace operation for name property
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value       |
		| Operation | Replace     |
		| Path      | /name       |
		| Value     | UpdatedName |
	When I validate the entry
	Then the validation should succeed

Scenario: Valid Replace operation for properties
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value                |
		| Operation | Replace              |
		| Path      | /properties/setting2 |
		| Value     | updatedValue         |
	When I validate the entry
	Then the validation should succeed

Scenario: Valid Remove operation for properties
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value                |
		| Operation | Remove               |
		| Path      | /properties/setting3 |
		| Value     | null                 |
	When I validate the entry
	Then the validation should succeed

Scenario Outline: Valid property paths
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value        |
		| Operation | Add          |
		| Path      | <Path>       |
		| Value     | testValue    |
	When I validate the entry
	Then the validation should succeed

Examples:
	| Path                              |
	| /properties/simple                |
	| /properties/with_underscore       |
	| /properties/with-dash             |
	| /properties/with123numbers        |
	| /properties/nested/path           |


Scenario Outline: Invalid Path values
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value     |
		| Operation | Add       |
		| Path      | <Path>    |
		| Value     | testValue |
	When I validate the entry
	Then the validation should fail
	And the validation error should contain "Path is required and must either be "/name" or start with "/properties/""

Examples:
	| Path              |
	|                   |
	| null              |
	| /invalid          |
	| /prop             |
	| /property         |
	| properties/test   |
	| /names            |
	| /name/invalid     |

Scenario: Remove operation with non-null value should fail
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value                |
		| Operation | Remove               |
		| Path      | /properties/setting1 |
		| Value     | shouldBeNull         |
	When I validate the entry
	Then the validation should fail
	And the validation error should contain "When Op is 'remove', Value must be left as null."

Scenario Outline: Add/Replace operations with null value should fail
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value        |
		| Operation | <Operation>  |
		| Path      | /name        |
		| Value     | null         |
	When I validate the entry
	Then the validation should fail
	And the validation error should contain "When Op is not 'remove', Value must be provided."

Examples:
	| Operation |
	| Add       |
	| Replace   |

Scenario: Multiple validation errors
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value     |
		| Operation | Add       |
		| Path      | /invalid  |
		| Value     | null      |
	When I validate the entry
	Then the validation should fail
	And the validation should have multiple errors
	And the validation error should contain "Path is required and must either be "/name" or start with "/properties/""
	And the validation error should contain "When Op is not 'remove', Value must be provided."

Scenario: Valid Add operation with complex object value
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value                    |
		| Operation | Add                      |
		| Path      | /properties/complexData  |
		| Value     | {"nested": "objectData"} |
	When I validate the entry
	Then the validation should succeed

Scenario: Valid Add operation with array value
	Given I have UpdateTenantJsonPatchEntry with:
		| Field     | Value                |
		| Operation | Add                  |
		| Path      | /properties/listData |
		| Value     | [1, 2, 3]            |
	When I validate the entry
	Then the validation should succeed