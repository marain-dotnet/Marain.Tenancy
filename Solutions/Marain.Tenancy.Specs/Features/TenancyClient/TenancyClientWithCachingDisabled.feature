@perFeatureContainer
@useTenancyApi
@useTenancyClient
@disableTenantCaching

Feature: TenancyClient with caching disabled
	In order to use Marain Tenant services
	As a developer
	I want to be able to access the standard ITenantProvider via the client API.

Scenario: Get a tenant that does not exist
	When I use the Tenancy Client to get a tenant with id "NotFound"
	Then it should throw a "ProblemDetails"

Scenario: Create a child tenant
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the Tenancy Client to get the tenant with the id called "ChildTenantId" and call it "Result"
	Then the tenant called "ChildTenant1" should have the same ID as the tenant called "Result"

Scenario: Get a tenant with an etag retrieved from a created tenant
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	And I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I get the ETag of the tenant called "ChildTenant1" and call it "ChildTenantETag"
	When I use the Tenancy Client to get the tenant with the id called "ChildTenantId" and the ETag called "ChildTenantETag"
	Then it should throw an ApiException with Response Status Code 304

Scenario: Get a tenant with an etag retrieved from a tenant got from the Get Tenant endpoint
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	And I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the Tenancy Client to get the tenant with the id called "ChildTenantId" and call it "Result"
	And I get the ETag of the tenant called "Result" and call it "ResultETag"
	When I use the Tenancy Client to get the tenant with the id called "ChildTenantId" and the ETag called "ResultETag"
	Then it should throw an ApiException with Response Status Code 304

Scenario: Update a child tenant
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	When I use the Tenancy Client to update the properties of the tenant called "ChildTenant1"
	| Key       | Value                           | Type            |
	| FirstKey  | 1                               | integer         |
	| SecondKey | This is a string                | string          |
	| ThirdKey  | 1999-01-17                      | datetimeoffset  |
	And I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the Tenancy Client to get the tenant with the id called "ChildTenantId" and call it "Result"
	Then the tenant called "ChildTenant1" should have the same ID as the tenant called "Result"
	And the tenant called "Result" should have the properties
	| Key       | Value                           | Type            |
	| FirstKey  | 1                               | integer         |
	| SecondKey | This is a string                | string          |
	| ThirdKey  | 1999-01-17                      | datetimeoffset  |

Scenario: Add, update, and remove properties of a child tenant
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	And I use the Tenancy Client to update the properties of the tenant called "ChildTenant1"
	| Key       | Value            | Type           |
	| FirstKey  | 1                | integer        |
	| SecondKey | This is a string | string         |
	| ThirdKey  | 1999-01-17       | datetimeoffset |
	When I use the Tenancy Client to rename the tenant called "ChildTenant1" to "RenamedChildTenant1" and update its properties
	| Property  | Value                | Type    | Action   |
	| FirstKey  | 2                    | integer | addOrSet |
	| FourthKey | 4                    | integer | addOrSet |
	| FifthKey  | This is a new string | string  | addOrSet |
	| ThirdKey  |                      |         | remove   |
	And I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I get the tenant with the id "ChildTenantId" and call it "Result"
	Then the tenant called "ChildTenant1" should have the same ID as the tenant called "Result"
	And the tenant called "Result" should now have the name "RenamedChildTenant1"
	And the tenant called "Result" should have the properties
	| Key       | Value                | Type    |
	| FirstKey  | 2                    | integer |
	| SecondKey | This is a string     | string  |
	| FourthKey | 4                    | integer |
	| FifthKey  | This is a new string | string  |

Scenario: Create a child of a child
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant2" and call it "ChildTenantId"
	And I use the Tenancy Client to get the tenant with the id called "ChildTenantId" and call it "Result"
	Then the tenant called "ChildTenant2" should have the same ID as the tenant called "Result"

Scenario: Get children when no child tenants exist using the parent tenant Id
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the Tenancy Client to get the children of the tenant with the id called "ChildTenantId" with maxItems 20 and call them "Result"
	Then there should be no links in the GetTenants link collection of the children called "Result"

Scenario: Get children when no child tenants exist using the parent tenant children link
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	And I get the children of the tenant called "ChildTenant1" using the children link and call them "Result"
	Then there should be no links in the GetTenants link collection of the children called "Result"

Scenario: Get children
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant3" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant4" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant5" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the Tenancy Client to get the children of the tenant with the id called "ChildTenantId" with maxItems 20 and call them "Result"
	Then the links in the GetTenants link collection of the children called "Result" should match the self links of the tenants called
	| TenantName   |
	| ChildTenant2 |
	| ChildTenant3 |
	| ChildTenant4 |
	| ChildTenant5 |
	And the children called "Result" should contain a self link
	And the children called "Result" should contain a next link with no value
	And the children called "Result" should have the ContinuationToken property set to null
	And the children called "Result" should have the MaxItems property set to 20

Scenario: Get children when there is a single child tenant
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the Tenancy Client to get the children of the tenant with the id called "ChildTenantId" with maxItems 20 and call them "Result"
	Then the links in the GetTenants link collection of the children called "Result" should match the self links of the tenants called
	| TenantName   |
	| ChildTenant2 |

Scenario: Get children with continuation token
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant3" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant4" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant5" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the Tenancy Client to get the children of the tenant with the id called "ChildTenantId" with maxItems 2 and call them "Result"
	And I use the Tenancy Client to get the children of the tenant with the id called "ChildTenantId" with maxItems 2 and continuation token from the children called "Result" and call them "Result2"
	Then the links in the GetTenants link collection of the children called "Result" should contain 2 items
	And the links in the GetTenants link collection of the children called "Result2" should contain 2 items
	Then the links in the GetTenants link collections of the children called "Result" and "Result2" should each match 2 of the self links of the tenants called
	| TenantName   |
	| ChildTenant5 |
	| ChildTenant4 |
	| ChildTenant3 |
	| ChildTenant2 |

Scenario: Get children with next link
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant3" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant4" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant5" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I use the Tenancy Client to get the children of the tenant with the id called "ChildTenantId" with maxItems 2 and call them "Result"
	And I use the Tenancy Client to get additional children using the next link from the children called "Result" and call them "Result2"
	Then the links in the GetTenants link collection of the children called "Result" should contain 2 items
	And the links in the GetTenants link collection of the children called "Result2" should contain 2 items
	Then the links in the GetTenants link collections of the children called "Result" and "Result2" should each match 2 of the self links of the tenants called
	| TenantName   |
	| ChildTenant5 |
	| ChildTenant4 |
	| ChildTenant3 |
	| ChildTenant2 |

Scenario: Delete a child using the parent and child tenant Ids
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant3" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant4" for the tenant called "ChildTenant1"
	And I create a child tenant called "ChildTenant5" for the tenant called "ChildTenant1"
	When I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenantId"
	And I get the tenant id of the tenant called "ChildTenant3" and call it "DeletedChildTenantId"
	And I use the Tenancy Client to delete the tenant with the id called "DeletedChildTenantId" that is a child of the tenant with the id called "ChildTenantId"
	And I use the Tenancy Client to get the children of the tenant with the id called "ChildTenantId" with maxItems 20 and call them "Result"
	Then the links in the GetTenants link collection of the children called "Result" should match the self links of the tenants called
	| TenantName   |
	| ChildTenant2 |
	| ChildTenant4 |
	| ChildTenant5 |

Scenario: Delete a child using the delete link from getting the children of the parent tenant
	Given I use the Tenancy Client to create a child tenant called "ChildTenant1" for the root tenant
	And I create a child tenant called "ChildTenant2" for the tenant called "ChildTenant1"
	And I get the tenant id of the tenant called "ChildTenant1" and call it "ChildTenant1Id"
	And I use the Tenancy Client to get the children of the tenant with the id called "ChildTenant1Id" with maxItems 2 and call them "Result"
	When I use the Tenancy Client to delete a tenant using the DeleteTenant link at position 0 from the children called "Result"
	And I use the Tenancy Client to get the children of the tenant with the id called "ChildTenant1Id" with maxItems 2 and call them "Result2"
	Then there should be no links in the GetTenants link collection of the children called "Result2"

Scenario: Root tenant has empty properties
	When I use the Tenancy Client to get the tenant with id "f26450ab1668784bb327951c8b08f347" and call it "Root"
	Then the tenant called "Root" should have no properties
	And the tenant called "Root" should have a self link with path "/f26450ab1668784bb327951c8b08f347/marain/tenant"
	And the tenant called "Root" should have a children link with path "/f26450ab1668784bb327951c8b08f347/marain/tenant/children"

Scenario: Updates to root tenant are prohibited
	When I use the Tenancy Client to update the properties of the tenant with id "f26450ab1668784bb327951c8b08f347"
	| Key       | Value            | Type           |
	| FirstKey  | 1                | integer        |
	Then it should throw an ApiException with Response Status Code 405
