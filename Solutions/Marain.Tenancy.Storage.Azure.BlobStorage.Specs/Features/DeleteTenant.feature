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

# TODO:
# Attempt to delete non-existent tenant
# Attempt to delete child for which parent is non-existent
# Attempt to delete a non-empty parent
# Delete child of a child?
# What happens if we try to delete the root?