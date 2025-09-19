extension microsoftGraphV1

param appRegistrationName string
param identifierUris string[] = []
param replyUrls string[] = []

var msGraphAppId = '00000003-0000-0000-c000-000000000000'
var tenancyAppRoles = [
  {
    id: '7619c293-764c-437b-9a8e-698a26250efd'
    displayName: 'Tenancy administrator'
    description: 'Ability to create, modify, read, and remove tenants'
    value: 'TenancyAdministrator'
    allowedMemberTypes: [
        'User'
        'Application'
    ]
  }
  {
    id: '60743a6a-63b6-42e5-a464-a08698a0e9ed'
    displayName: 'Tenancy reader'
    description: 'Ability to read information about tenants'
    value: 'TenancyReader'
    allowedMemberTypes: [
        'User'
        'Application'
    ]
  }
]

resource tenancy_app_reg 'Microsoft.Graph/applications@v1.0' = {
  displayName: appRegistrationName
  appRoles: tenancyAppRoles
  uniqueName: appRegistrationName
  identifierUris: identifierUris
  web: {
    implicitGrantSettings: {
      enableAccessTokenIssuance: false
      enableIdTokenIssuance: false
    }
    redirectUris: replyUrls
  }
  requiredResourceAccess: [
    {
      resourceAccess: [
        {
          id: '37f7f235-527c-4136-accd-4a02d197296e' // openid
          type: 'Scope'
        }
      ]
      resourceAppId: msGraphAppId
    }
  ]
}

output clientId string = tenancy_app_reg.appId
