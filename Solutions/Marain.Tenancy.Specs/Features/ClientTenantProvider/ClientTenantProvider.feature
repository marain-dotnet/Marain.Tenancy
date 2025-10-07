@perFeatureContainer
@useTenancyApi
@useTenancyClient
@useClientTenantProvider
@disableTenantCaching

Feature: Tenancy Api accessed via the Client Tenant Provider
	In order to use Marain Tenant services
	As a developer
	I want to be able to use an implementation of ITenantStore that uses the Tenancy Api.

Scenario: Get a tenant that does not exist
	When I use the ClientTenantProvider to get a tenant with id "NotFound"
	Then it should throw a "TenantNotFoundException"

Scenario: Create a child of the root tenant
	When I use the ClientTenantProvider to create a child tenant called "ChildTenant1" for the root tenant
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the ClientTenantProvider to get the tenant with the id called "ChildTenantId" and call it "Result"
	Then the tenant called "ChildTenant1" should have the same ID as the tenant called "Result"

Scenario: Get a tenant with an etag retrieved from a created tenant
	Given I use the ClientTenantProvider to create a child tenant called "ChildTenant1" for the root tenant
	And I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I get the ETag of the tenant called "ChildTenant1" and call it "ChildTenantETag"
	When I use the ClientTenantProvider to get the tenant with the id called "ChildTenantId" and the ETag called "ChildTenantETag" and call it "Result"

Scenario: Get a tenant with an etag retrieved from a tenant got from the Get Tenant endpoint
	Given I use the ClientTenantProvider to create a child tenant called "ChildTenant1" for the root tenant
	And I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the ClientTenantProvider to get the tenant with the id called "ChildTenantId" and call it "Result"
	And I get the ETag of the tenant called "Result" and call it "ResultETag"
	When I use the ClientTenantProvider to get the tenant with the id called "ChildTenantId" and the ETag called "ResultETag" and call it "Result"
	Then it should throw a "TenantNotModifiedException"

Scenario: Update a child tenant
	Given I use the ClientTenantProvider to create a child tenant called "ChildTenant1" for the root tenant
	When I use the ClientTenantProvider to update the properties of the tenant called "ChildTenant1"
	| Key       | Value                           | Type            | Action  |
	| FirstKey  | 1                               | integer         | Add     |
	| SecondKey | This is a string                | string          | Add     |
	| ThirdKey  | 1999-01-17                      | datetimeoffset  | Add     |
	And I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the ClientTenantProvider to get the tenant with the id called "ChildTenantId" and call it "Result"
	Then the tenant called "ChildTenant1" should have the same ID as the tenant called "Result"
	And the tenant called "Result" should have the properties
	| Key       | Value                           | Type            |
	| FirstKey  | 1                               | integer         |
	| SecondKey | This is a string                | string          |
	| ThirdKey  | 1999-01-17                      | datetimeoffset  |


Scenario: Create a child of a child
	Given I use the ClientTenantProvider to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant2" and call it "ChildTenantId"
	And I use the ClientTenantProvider to get the tenant with the id called "ChildTenantId" and call it "Result"
	Then the tenant called "ChildTenant2" should have the same ID as the tenant called "Result"

Scenario: Get children when no child tenants exist using the parent tenant Id
	Given I use the ClientTenantProvider to create a child tenant called "ChildTenant1" for the root tenant
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the ClientTenantProvider to get the children of the tenant with the id called "ChildTenantId" with limit 20 and call them "Result"
	Then there should be no Ids in the list of child tenant Ids in the children called "Result"

Scenario: Get children
	Given I use the ClientTenantProvider to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant3" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant4" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant5" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the ClientTenantProvider to get the children of the tenant with the id called "ChildTenantId" with limit 20 and call them "Result"
	Then the Ids in the list of child tenant Ids in the children called "Result" should match the Ids of the tenants called
	| TenantName   |
	| ChildTenant2 |
	| ChildTenant3 |
	| ChildTenant4 |
	| ChildTenant5 |
	And the children called "Result" should have the ContinuationToken property set to null

Scenario: Get children when there is a single child tenant
	Given I use the ClientTenantProvider to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the ClientTenantProvider to get the children of the tenant with the id called "ChildTenantId" with limit 20 and call them "Result"
	Then the Ids in the list of child tenant Ids in the children called "Result" should match the Ids of the tenants called
	| TenantName   |
	| ChildTenant2 |

Scenario: Get children with continuation token
	Given I use the ClientTenantProvider to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant3" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant4" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant5" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the ClientTenantProvider to get the children of the tenant with the id called "ChildTenantId" with limit 2 and call them "Result"
	And I use the ClientTenantProvider to get the children of the tenant with the id called "ChildTenantId" with limit 2 and continuation token from the children called "Result" and call them "Result2"
	Then the Ids in the list of child tenant Ids in the children called "Result" should contain 2 items
	And the Ids in the list of child tenant Ids in the children called "Result2" should contain 2 items
	Then the Ids in the lists of child tenant Ids in the children called "Result" and "Result2" should each match 2 of the self Ids of the tenants called
	| TenantName   |
	| ChildTenant5 |
	| ChildTenant4 |
	| ChildTenant3 |
	| ChildTenant2 |

Scenario: Delete a child using the child tenant Id
	Given I use the ClientTenantProvider to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant3" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant4" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant5" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I get the tenant id of the tenant called "ChildTenant3" and call it "DeletedChildTenantId"
	And I use the ClientTenantProvider to delete the tenant with the id called "DeletedChildTenantId"
	And I use the ClientTenantProvider to get the children of the tenant with the id called "ChildTenantId" with limit 20 and call them "Result"
	Then the Ids in the list of child tenant Ids in the children called "Result" should match the Ids of the tenants called
	| TenantName   |
	| ChildTenant2 |
	| ChildTenant4 |
	| ChildTenant5 |
