@perScenarioContainer
@withBlobStorageTenantProvider

Feature: DeleteTenant
    In order to manage tenants and their configuration
    As a tenant owner
    I want to be able to delete existing tenants

Scenario: Delete a child
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ParentTenant'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant2' labelled 'ChildTenant2'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant3' labelled 'ChildTenant3'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant4' labelled 'ChildTenant4'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant5' labelled 'ChildTenant5'
    When I delete the tenant with the id from label 'ChildTenant3'
    And I get the children of the tenant with the label 'ParentTenant' with maxItems 20
    Then the ids of all the children across all pages should match these tenant ids
    | TenantName   |
    | ChildTenant2 |
    | ChildTenant4 |
    | ChildTenant5 |

Scenario: Delete a child of a child
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ParentTenant'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant2' labelled 'ChildTenant2'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant3' labelled 'ChildTenant3'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant4' labelled 'ChildTenant4'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant5' labelled 'ChildTenant5'
    And the tenant labelled 'ChildTenant2' has a child tenant called 'ChildTenant6' labelled 'ChildTenant6'
    And the tenant labelled 'ChildTenant3' has a child tenant called 'ChildTenant7' labelled 'ChildTenant7'
    And the tenant labelled 'ChildTenant3' has a child tenant called 'ChildTenant8' labelled 'ChildTenant8'
    And the tenant labelled 'ChildTenant3' has a child tenant called 'ChildTenant9' labelled 'ChildTenant9'
    When I delete the tenant with the id from label 'ChildTenant8'
    And I get the children of the tenant with the label 'ChildTenant3' with maxItems 20
    Then the ids of all the children across all pages should match these tenant ids
    | TenantName   |
    | ChildTenant7 |
    | ChildTenant9 |

Scenario: Delete a non-existent child of root
    When I attempt to delete a tenant with the id that looks like a child of the root tenant but which does not exist
    Then the attempt to delete a tenant should throw a TenantNotFoundException

Scenario: Delete a non-existent child of non-existent child of root
    When I attempt to delete a tenant with the id that looks like a child of a child of the root tenant, where neither exists
    Then the attempt to delete a tenant should throw a TenantNotFoundException

Scenario: Delete a non-existent child of existing parent
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ParentTenant'
    When I attempt to delete a tenant with the id that looks like a child of 'ParentTenant' but which does not exist
    Then the attempt to delete a tenant should throw a TenantNotFoundException

Scenario: Delete root tenant
    When I attempt to delete the root tenant
    Then the attempt to delete a tenant should throw an ArgumentException

Scenario: Delete a non-empty parent
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ParentTenant'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant2' labelled 'ChildTenant2'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant3' labelled 'ChildTenant3'
    When I attempt to delete the tenant with the id from label 'ParentTenant'
    Then the attempt to delete a tenant should throw an ArgumentException
