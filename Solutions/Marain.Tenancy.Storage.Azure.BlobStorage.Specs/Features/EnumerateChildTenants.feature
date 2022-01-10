@perScenarioContainer
@withBlobStorageTenantProvider

Feature: EnumerateChildTenants
    In order to manage tenants and their configuration
    As a tenant owner
    I want to be able to retrieve stored tenant information

Background:
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ParentTenant'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant2' labelled 'ChildTenant2'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant3' labelled 'ChildTenant3'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant4' labelled 'ChildTenant4'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant5' labelled 'ChildTenant5'

Scenario: Get children
    When I get the children of the tenant with the label 'ParentTenant' with maxItems 20
    Then the ids of the children for page 0 should match these tenant ids
    | TenantName   |
    | ChildTenant2 |
    | ChildTenant3 |
    | ChildTenant4 |
    | ChildTenant5 |
    And the continuation token for page 0 should be null

Scenario: Get children paginated
    When I get the children of the tenant with the label 'ParentTenant' with maxItems 2
    And I continue getting the children of the tenant with the label 'ParentTenant' with maxItems 20
    Then page 0 returned by GetChildrenAsync should contain 2 items
    And the continuation token for page 0 should not be null
    And page 1 returned by GetChildrenAsync should contain 2 items
    And the continuation token for page 1 should be null
    And the ids of all the children across all pages should match these tenant ids
    | TenantName   |
    | ChildTenant2 |
    | ChildTenant3 |
    | ChildTenant4 |
    | ChildTenant5 |
