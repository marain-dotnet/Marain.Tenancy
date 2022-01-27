@perFeatureContainer
@withTenancyClient
@useTenancyFunction

Feature: Tenancy Api
	In order to use Marain Tenant services
	As a developer
	I want to be able to access the Tenancy Api.

Scenario: Get the OpenApi definition for the Api
	When I request the tenancy service endpoint '/swagger'
	Then I receive a 'OK' response

Scenario: Get a tenant that does not exist
	When I request the tenant with Id 'thistenantdoesnotexist' from the API
	Then I receive a 'NotFound' response

Scenario: Create a tenant
	When I use the API to create a new tenant
	| ParentTenantId                   | Name |
	| f26450ab1668784bb327951c8b08f347 | Test |
	Then I receive a 'Created' response
	And the response should contain a Location header

Scenario: Get the root tenant
	When I request the tenant with Id 'f26450ab1668784bb327951c8b08f347' from the API
	Then I receive an 'OK' response
	And the response content should have a string property called 'name' with value 'Root'
	And the response should not contain an Etag header
	And the response should contain a Cache-Control header with value 'max-age=300'

Scenario: Retrieve a newly created tenant using the location header returned from the create request
	Given I have used the API to create a new tenant
	| ParentTenantId                   | Name |
	| f26450ab1668784bb327951c8b08f347 | Test |
	When I request the tenant using the Location from the previous response
	Then I receive an 'OK' response
	And the response content should have a string property called 'name' with value 'Test'
	And the response should contain an Etag header
	And the response should contain a Cache-Control header with value 'max-age=300'

Scenario: Request a tenant using the etag from a previous get tenant request
	Given I have used the API to create a new tenant
	| ParentTenantId                   | Name |
	| f26450ab1668784bb327951c8b08f347 | Test |
	And I store the id from the response Location header as 'New tenant ID'
	And I have requested the tenant with the ID called 'New tenant ID'
	When I request the tenant using the ID called 'New tenant ID' and the Etag from the previous response
	Then I receive a 'NotModified' response
