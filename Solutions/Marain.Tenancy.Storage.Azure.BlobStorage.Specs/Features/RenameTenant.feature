@perScenarioContainer
@withBlobStorageTenantProvider

Feature: RenameTenant
    In order to manage tenants and their configuration
    As a tenant owner
    I want to be able to change the name of an existing tenant

Scenario: Rename a child tenant
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ChildTenant1'
    When I change the name of the tenant labelled 'ChildTenant1' to 'NewName' and label the returned tenant 'UpdateResult'
    And I get the tenant with the id from label 'ChildTenant1' labelled 'GetResult'
    Then the tenant labelled 'UpdateResult' should have the same ID as the tenant labelled 'ChildTenant1'
    And the tenant labelled 'GetResult' should have the same ID as the tenant labelled 'ChildTenant1'
    And the tenant labelled 'UpdateResult' should have the name 'NewName'
    And the tenant labelled 'GetResult' should have the name 'NewName'

# TODO:
# Try to rename a non-existent tenant
# Try to rename a child for which parent is non-existent
# Rename child of child?
# What if we try to rename the root?