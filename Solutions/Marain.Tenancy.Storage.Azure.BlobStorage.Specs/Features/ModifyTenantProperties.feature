@perScenarioContainer
@withBlobStorageTenantProvider

Feature: ModifyTenantProperties
    In order to manage tenants and their configuration
    As a tenant owner
    I want to be able to modify the properties stored in tenants

Scenario: Add properties to a child tenant that has no properties
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ChildTenant1'
    When I update the properties of the tenant labelled 'ChildTenant1' and label the returned tenant 'Updated'
    | Key       | Value            | Type           |
    | FirstKey  | 1                | integer        |
    | SecondKey | This is a string | string         |
    | ThirdKey  | 1999-01-17       | datetimeoffset |
    And I get the tenant with the id from label 'ChildTenant1' labelled 'Result'
    Then the tenant labelled 'ChildTenant1' should have the same ID as the tenant labelled 'Result'
    And the tenant labelled 'Result' should have the properties
    | Key       | Value            | Type           |
    | FirstKey  | 1                | integer        |
    | SecondKey | This is a string | string         |
    | ThirdKey  | 1999-01-17       | datetimeoffset |

Scenario: Modify properties of a child tenant
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ChildTenant1' with these properties
    | Key       | Value            | Type           |
    | FirstKey  | 1                | integer        |
    | SecondKey | This is a string | string         |
    | ThirdKey  | 1999-01-17       | datetimeoffset |
    When I update the properties of the tenant labelled 'ChildTenant1' and label the returned tenant 'UpdateResult'
    | Key       | Value                      | Type    |
    | FirstKey  | 2                          | integer |
    | SecondKey | This is a different string | string  |
    And I get the tenant with the id from label 'ChildTenant1' labelled 'Result'
    Then the tenant labelled 'ChildTenant1' should have the same ID as the tenant labelled 'Result'
    And the tenant labelled 'UpdateResult' should have the properties
    | Key       | Value                      | Type           |
    | FirstKey  | 2                          | integer        |
    | SecondKey | This is a different string | string         |
    | ThirdKey  | 1999-01-17                 | datetimeoffset |
    And the tenant labelled 'Result' should have the properties
    | Key       | Value                      | Type           |
    | FirstKey  | 2                          | integer        |
    | SecondKey | This is a different string | string         |
    | ThirdKey  | 1999-01-17                 | datetimeoffset |

Scenario: Add properties to a child tenant that already has properties
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ChildTenant1' with these properties
    | Key       | Value            | Type           |
    | FirstKey  | 1                | integer        |
    | SecondKey | This is a string | string         |
    | ThirdKey  | 1999-01-17       | datetimeoffset |
    When I update the properties of the tenant labelled 'ChildTenant1' and label the returned tenant 'UpdateResult'
    | Key       | Value                      | Type    |
    | FourthKey | 2                          | integer |
    | FifthKey  | This is a different string | string  |
    And I get the tenant with the id from label 'ChildTenant1' labelled 'Result'
    Then the tenant labelled 'ChildTenant1' should have the same ID as the tenant labelled 'Result'
    And the tenant labelled 'UpdateResult' should have the properties
    | Key       | Value                      | Type           |
    | FirstKey  | 1                          | integer        |
    | SecondKey | This is a string           | string         |
    | ThirdKey  | 1999-01-17                 | datetimeoffset |
    | FourthKey | 2                          | integer        |
    | FifthKey  | This is a different string | string         |
    And the tenant labelled 'Result' should have the properties
    | Key       | Value                      | Type           |
    | FirstKey  | 1                          | integer        |
    | SecondKey | This is a string           | string         |
    | ThirdKey  | 1999-01-17                 | datetimeoffset |
    | FourthKey | 2                          | integer        |
    | FifthKey  | This is a different string | string         |

Scenario: Remove properties from a child tenant
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ChildTenant1' with these properties
    | Key       | Value            | Type           |
    | FirstKey  | 1                | integer        |
    | SecondKey | This is a string | string         |
    | ThirdKey  | 1999-01-17       | datetimeoffset |
    When I remove the 'SecondKey' property of the tenant labelled 'ChildTenant1' and label the returned tenant 'UpdateResult'
    And I get the tenant with the id from label 'ChildTenant1' labelled 'Result'
    Then the tenant labelled 'ChildTenant1' should have the same ID as the tenant labelled 'Result'
    And the tenant labelled 'Result' should have the properties
    | Key       | Value            | Type           |
    | FirstKey  | 1                | integer        |
    | ThirdKey  | 1999-01-17       | datetimeoffset |
    And the tenant labelled 'UpdateResult' should have the properties
    | Key       | Value            | Type           |
    | FirstKey  | 1                | integer        |
    | ThirdKey  | 1999-01-17       | datetimeoffset |

Scenario: Add, modify, and remove properties of a child tenant that already has properties
    Given the root tenant has a child tenant called 'ChildTenant1' labelled 'ChildTenant1' with these properties
    | Key       | Value            | Type           |
    | FirstKey  | 1                | integer        |
    | SecondKey | This is a string | string         |
    | ThirdKey  | 1999-01-17       | datetimeoffset |
    When I update the properties of the tenant labelled 'ChildTenant1' and remove the 'ThirdKey' property and call the returned tenant 'UpdateResult'
    | Key       | Value                      | Type    |
    | SecondKey | This is a different string | string  |
    | FourthKey | 2                          | integer |
    | FifthKey  | This is a new string       | string  |
    And I get the tenant with the id from label 'ChildTenant1' labelled 'Result'
    Then the tenant labelled 'ChildTenant1' should have the same ID as the tenant labelled 'Result'
    And the tenant labelled 'UpdateResult' should have the properties
    | Key       | Value                      | Type           |
    | FirstKey  | 1                          | integer        |
    | SecondKey | This is a different string | string         |
    | FourthKey | 2                          | integer        |
    | FifthKey  | This is a new string       | string         |
    And the tenant labelled 'Result' should have the properties
    | Key       | Value                      | Type           |
    | FirstKey  | 1                          | integer        |
    | SecondKey | This is a different string | string         |
    | FourthKey | 2                          | integer        |
    | FifthKey  | This is a new string       | string         |
