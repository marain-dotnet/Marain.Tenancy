@perScenarioContainer
@withBlobStorageTenantProvider

Feature: CreateTenant
    In order to manage tenants and their configuration
    As a tenant owner
    I want to be able to create new tenants as children of tenants I control


Scenario: Create a child of the root tenant
    When I create a child tenant of the root tenant called 'ChildTenant1' labelled 'ChildTenant'
    And I get the tenant with the id from label 'ChildTenant' labelled 'Result'
    Then the tenant details labelled 'ChildTenant' should match the tenant details labelled 'Result'
    And the tenant labelled 'ChildTenant' should have storage configuration equivalent to the root

Scenario: Create a child of the root tenant with a well known Id
    Given a well-known tenant Guid labelled 'WellKnown1'
    When I create a well known (from the Guid labelled 'WellKnown1') child tenant of the root tenant called 'ChildTenant1' labelled 'ChildTenant'
    And I get the tenant with the id from label 'ChildTenant' labelled 'Result'
    Then the tenant details labelled 'ChildTenant' should match the tenant details labelled 'Result'
    And the tenant details labelled 'ChildTenant' should have tenant Id that is the hash of the Guid labelled 'WellKnown1'
    And the tenant labelled 'ChildTenant' should have storage configuration equivalent to the root

Scenario: Create a child of a child of the root tenant
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'Tenant1'
    When I create a child of the tenant labelled 'Tenant1' named 'ChildTenant2' labelled 'Tenant2'
    And I get the tenant with the id from label 'Tenant2' labelled 'Result'
    Then the tenant details labelled 'Tenant2' should match the tenant details labelled 'Result'
    And the tenant labelled 'Tenant2' should have storage configuration equivalent to the root

Scenario: Create a child of a child with well known Ids
    Given a well-known tenant Guid labelled 'WellKnown1'
    And a well-known tenant Guid labelled 'WellKnown2'
    And the root tenant has a well-known (from the Guid labelled 'WellKnown1') child tenant called 'ChildTenant1' labelled 'Tenant1'
    When I create a well known (from the Guid labelled 'WellKnown2') child of the tenant labelled 'Tenant1' named 'ChildTenant2' labelled 'Tenant2'
    And I get the tenant with the id from label 'Tenant2' labelled 'Result'
    Then the tenant details labelled 'Tenant2' should match the tenant details labelled 'Result'
    And the tenant details labelled 'Tenant1' should have tenant Id that is the hash of the Guid labelled 'WellKnown1'
    And the tenant details labelled 'Tenant2' should have tenant Id that is the concatenated hashes of the Guids labelled 'WellKnown1' and 'WellKnown2'
    And the tenant labelled 'Tenant2' should have storage configuration equivalent to the root

Scenario: Creating a child of a child with a well known Id that is already in use by a child of the same parent throws an ArgumentException
    Given a well-known tenant Guid labelled 'WellKnown1'
    And a well-known tenant Guid labelled 'WellKnown2'
    And the root tenant has a well-known (from the Guid labelled 'WellKnown1') child tenant called 'ChildTenant1' labelled 'Tenant1'
    And the tenant labelled 'Tenant1' has a well-known (from the Guid labelled 'WellKnown2') child tenant called 'ChildTenant2' labelled 'Tenant2'
    When I try to create a well known child (from the Guid labelled 'WellKnown2') of the tenant labelled 'Tenant1' named 'ChildTenant3'
    Then CreateWellKnownChildTenantAsync thould throw an ArgumentException

Scenario: Creating children that have the same well known Ids under different parents succeeds
    Given a well-known tenant Guid labelled 'WellKnown1'
    And a well-known tenant Guid labelled 'WellKnown2'
    And a well-known tenant Guid labelled 'WellKnown3'
    And a well-known tenant Guid labelled 'WellKnown4'
    And the root tenant has a well-known (from the Guid labelled 'WellKnown1') child tenant called 'ChildTenant1' labelled 'Parent1'
    And the root tenant has a well-known (from the Guid labelled 'WellKnown2') child tenant called 'ChildTenant2' labelled 'Parent2'
    And the tenant labelled 'Parent1' has a well-known (from the Guid labelled 'WellKnown3') child tenant called 'ChildTenant3' labelled 'ChildOfParent1'
    When I create a well known (from the Guid labelled 'WellKnown4') child of the tenant labelled 'Parent2' named 'ChildTenant4' labelled 'ChildOfParent2'
    And I get the tenant with the id from label 'ChildOfParent1' labelled 'Result1'
    And I get the tenant with the id from label 'ChildOfParent2' labelled 'Result2'
    Then the tenant details labelled 'Result1' should have tenant Id that is the concatenated hashes of the Guids labelled 'WellKnown1' and 'WellKnown3'
    And the tenant details labelled 'Result2' should have tenant Id that is the concatenated hashes of the Guids labelled 'WellKnown2' and 'WellKnown4'
    And the tenant labelled 'Result1' should have storage configuration equivalent to the root
    And the tenant labelled 'Result2' should have storage configuration equivalent to the root
