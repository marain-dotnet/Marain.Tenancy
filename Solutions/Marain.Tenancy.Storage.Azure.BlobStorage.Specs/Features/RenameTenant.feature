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

Scenario: Rename a child of a child
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ParentTenant'
    And the tenant labelled 'ParentTenant' has a child tenant called 'ChildTenant2' labelled 'ChildTenant2'
    When I change the name of the tenant labelled 'ChildTenant2' to 'NewName' and label the returned tenant 'UpdateResult'
    And I get the tenant with the id from label 'ChildTenant2' labelled 'GetResult'
    Then the tenant labelled 'UpdateResult' should have the same ID as the tenant labelled 'ChildTenant2'
    And the tenant labelled 'GetResult' should have the same ID as the tenant labelled 'ChildTenant2'
    And the tenant labelled 'UpdateResult' should have the name 'NewName'
    And the tenant labelled 'GetResult' should have the name 'NewName'

Scenario: Rename a non-existent child of root tenant
    When I attempt to change the name of a tenant with the id that looks like a child of the root tenant but which does not exist
    Then the attempt to change the name of a tenant should throw a TenantNotFoundException

Scenario: Rename a non-existent child of non-existent child of root
    When I attempt to change the name of a tenant with the id that looks like a child of a child of the root tenant, where neither exists
    Then the attempt to change the name of a tenant should throw a TenantNotFoundException

Scenario: Rename a non-existent child of existing parent
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ParentTenant'
    When I attempt to change the name of a tenant with the id that looks like a child of 'ParentTenant' but which does not exist
    Then the attempt to change the name of a tenant should throw a TenantNotFoundException

Scenario: Rename root tenant
    When I attempt to change the name of the root tenant
    Then the attempt to change the name of a tenant should throw an ArgumentException
