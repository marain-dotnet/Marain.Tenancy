@perFeatureContainer
@withTenancyClient
@useTenancyFunction

# Note that this set of specs does not include "create, update and retrieve" scenarios that are present in the
# "TenancyClient with caching disabled" specs. This is because we don't recommend using caching in "management
# plane" scenarios where tenants are being created, updated and so on.
Feature: TenancyClient with caching enabled
	In order to use Marain Tenant services
	As a developer
	I want to be able to access the standard ITenantProvider via the client API.

Scenario: Get a tenant that does not exist
	When I get a tenant with id "NotFound"
	Then it should throw a TenantNotFoundException

Scenario: Getting a tenant that has already been retrieved uses the cache
	Given I create a child tenant called "ChildTenant1" for the root tenant
	And I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	When I use the client to get the tenant with the id called "ChildTenantId" and call the response "Response1"
	Then the tenant response called "Response1" was retrieved from the cache

Scenario: Get a tenant with an etag retrieved from a created tenant
	Given I create a child tenant called "ChildTenant1" for the root tenant
	And I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I get the ETag of the tenant called "ChildTenant1" and call it "ChildTenantETag"
	When I get the tenant with the id called "ChildTenantId" and the ETag called "ChildTenantETag"
	Then it should throw a TenantNotModifiedException

Scenario: Get a tenant with an etag retrieved from a tenant got from the repo
	Given I create a child tenant called "ChildTenant1" for the root tenant
	And I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I get the tenant with the id called "ChildTenantId" and call it "Result"
	And I get the ETag of the tenant called "Result" and call it "ResultETag"
	When I get the tenant with the id called "ChildTenantId" and the ETag called "ResultETag"
	Then it should throw a TenantNotModifiedException

Scenario: Get children
	Given I create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant3" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant4" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant5" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I get the children of the tenant with the id called "ChildTenantId" with maxItems 20 and call them "Result"
	Then the ids of the children called "Result" should match the ids of the tenants called
	| TenantName   |
	| ChildTenant2 |
	| ChildTenant3 |
	| ChildTenant4 |
	| ChildTenant5 |

Scenario: Get children when no child tenants exist
	Given I create a child tenant called "ChildTenant1" for the root tenant
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I get the children of the tenant with the id called "ChildTenantId" with maxItems 20 and call them "Result"
	Then there should be no ids in the children called "Result"

Scenario: Get children when there is a single child tenant
	Given I create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I get the children of the tenant with the id called "ChildTenantId" with maxItems 20 and call them "Result"
	Then the ids of the children called "Result" should match the ids of the tenants called
	| TenantName   |
	| ChildTenant2 |

Scenario: Get children with continuation token
	Given I create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant3" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant4" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant5" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I get the children of the tenant with the id called "ChildTenantId" with maxItems 2 and call them "Result"
	And I get the children of the tenant with the id called "ChildTenantId" with maxItems 20 and continuation token "Result" and call them "Result2"
	Then there should be 2 tenants in "Result"
	And there should be 2 tenants in "Result2"
	And the ids of the children called "Result" and "Result2" should each match 2 of the ids of the tenants called
	| TenantName   |
	| ChildTenant5 |
	| ChildTenant4 |
	| ChildTenant3 |
	| ChildTenant2 |

Scenario: Root tenant has empty properties
	When I get the tenant with id "f26450ab1668784bb327951c8b08f347" and call it "Root"
	Then the tenant called "Root" should have no properties
