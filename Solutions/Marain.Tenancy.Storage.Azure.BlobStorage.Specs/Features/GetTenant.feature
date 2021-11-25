@perScenarioContainer
@withBlobStorageTenantProvider

Feature: GetTenant
    In order to manage tenants and their configuration
    As a tenant owner
    I want to be able to retrieve stored tenant information

Scenario: Get the root tenant when the root container does not exist yet
    When I get a tenant with the root id labelled 'RootTenant'
    Then the tenant details labelled 'RootTenant' should have the root tenant id
    And the tenant details labelled 'RootTenant' should have the root tenant name
    # TODO: should we report an ETag for the root tenant?

Scenario Outline: Get a child tenant when the root container does not exist yet
    When I try to get a tenant with id '<tenantId>'
    Then GetTenantAsync should throw a TenantNotFoundException

    Examples: 
    | tenantId                         |
    | 156cf35866d7594b9913e21bd23b90b0 |
    | NotValidTenantName               |

Scenario Outline: Get a child tenant when the root container does exist
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'Tenant1'
    When I try to get a tenant with id '<tenantId>'
    Then GetTenantAsync should throw a TenantNotFoundException

    Examples: 
    | tenantId                         |
    | 156cf35866d7594b9913e21bd23b90b0 |
    | NotValidTenantName               |

Scenario: Get a tenant that does not exist
    When I try to get a tenant with id 'NotFound'
    Then GetTenantAsync should throw a TenantNotFoundException

Scenario: Get a tenant that does exist without using an ETag
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ChildTenantId'
    When I get the tenant with the details called 'ChildTenantId' labelled 'Result'
    Then the tenant details labelled 'Result' should match the tenant details labelled 'ChildTenantId'

Scenario: Get a tenant that does exist using an ETag
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ChildTenantId'
    When I try to get the tenant using the details called 'ChildTenantId' passing the ETag
    Then GetTenantAsync should throw a TenantNotModifiedException

Scenario: Get a tenant with an etag retrieved from a tenant got from the repo
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ChildTenantId'
    And I get the tenant with the id from label 'ChildTenantId' labelled 'Result'
    When I try to get the tenant using the details called 'Result' passing the ETag
    Then GetTenantAsync should throw a TenantNotModifiedException
