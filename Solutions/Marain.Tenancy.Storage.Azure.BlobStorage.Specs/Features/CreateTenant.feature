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

Scenario: Create a child of the root tenant with a well known Id
    When I create a well known child tenant of the root tenant called 'ChildTenant1' with a Guid of 'F446F305-993B-49A4-B5FA-010EE2AF0FA2' labelled 'ChildTenant'
    And I get the tenant with the id from label 'ChildTenant' labelled 'Result'
    Then the tenant details labelled 'ChildTenant' should match the tenant details labelled 'Result'
    And the tenant details labelled 'ChildTenant' should have tenant Id '05f346f43b99a449b5fa010ee2af0fa2'

Scenario: Create a child of a child of the root tenant
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'Tenant1'
    When I create a child of the tenant labelled 'Tenant1' named 'ChildTenant2' labelled 'Tenant2'
    And I get the tenant with the id from label 'Tenant2' labelled 'Result'
    Then the tenant details labelled 'Tenant2' should match the tenant details labelled 'Result'

Scenario: Create a child of a child with well known Ids
    Given the root tenant has a well-known child tenant called 'ChildTenant1' with a Guid of 'EE17B20B-B372-4493-8145-9DD95516B9AF' labelled 'Tenant1'
    When I create a well known child of the tenant labelled 'Tenant1' named 'ChildTenant2' with a Guid of 'DD045C05-E7FB-4214-8878-F9E7CA9B0F5F' labelled 'Tenant2'
    And I get the tenant with the id from label 'Tenant2' labelled 'Result'
    Then the tenant details labelled 'Tenant2' should match the tenant details labelled 'Result'
    And the tenant details labelled 'Tenant1' should have tenant Id '0bb217ee72b3934481459dd95516b9af'
    And the tenant details labelled 'Tenant2' should have tenant Id '0bb217ee72b3934481459dd95516b9af055c04ddfbe714428878f9e7ca9b0f5f'

Scenario: Creating a child of a child with a well known Id that is already in use by a child of the same parent throws an ArgumentException
    Given the root tenant has a well-known child tenant called 'ChildTenant1' with a Guid of 'ABE7C6C9-8494-4797-B52E-5C7B3EF1CE56' labelled 'Tenant1'
    And the tenant labelled 'Tenant1' has a well-known child tenant called 'ChildTenant2' with a Guid of 'DD045C05-E7FB-4214-8878-F9E7CA9B0F5F' labelled 'Tenant2'
    When I try to create a well known child of the tenant labelled 'Tenant1' named 'ChildTenant3' with a Guid of 'DD045C05-E7FB-4214-8878-F9E7CA9B0F5F'
    Then CreateWellKnownChildTenantAsync thould throw an ArgumentException

Scenario: Creating children that have the same well known Ids under different parents succeeds
    Given the root tenant has a well-known child tenant called 'ChildTenant1' with a Guid of '2A182D6E-FF13-4A73-87AF-0B58D8243603' labelled 'Parent1'
    And the root tenant has a well-known child tenant called 'ChildTenant2' with a Guid of '086D75A1-DA07-4C90-BEA7-857A6C126280' labelled 'Parent2'
    And the tenant labelled 'Parent1' has a well-known child tenant called 'ChildTenant3' with a Guid of '2A182D6E-FF13-4A73-87AF-0B58D8243603' labelled 'ChildOfParent1'
    When I create a well known child of the tenant labelled 'Parent2' named 'ChildTenant4' with a Guid of '2A182D6E-FF13-4A73-87AF-0B58D8243603' labelled 'ChildOfParent2'
    And I get the tenant with the id from label 'ChildOfParent1' labelled 'Result1'
    And I get the tenant with the id from label 'ChildOfParent2' labelled 'Result2'
    Then the tenant details labelled 'Result1' should have tenant Id '6e2d182a13ff734a87af0b58d82436036e2d182a13ff734a87af0b58d8243603'
    And the tenant details labelled 'Result2' should have tenant Id 'a1756d0807da904cbea7857a6c1262806e2d182a13ff734a87af0b58d8243603'
