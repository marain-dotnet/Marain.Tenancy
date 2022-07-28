param functionName string
@allowed([
  'AllowAnonymous'
  'RedirectToLoginPage'
  'Return401'
  'Return403'
])
param unauthenticatedClientAction string = 'RedirectToLoginPage'
param aadClientId string
param authEnabled bool = true


targetScope = 'resourceGroup'


resource function_app 'Microsoft.Web/sites@2021-02-01' existing = {
  name: functionName
}

resource authSettingsV2 'Microsoft.Web/sites/config@2022-03-01' = {
  name: 'authsettingsV2'
  parent: function_app
  properties: {
    platform: {
      enabled: authEnabled
    }
    globalValidation: {
      unauthenticatedClientAction: unauthenticatedClientAction
      redirectToProvider: 'azureactivedirectory'
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        isAutoProvisioned: false  // included to reduce noise in -WhatIf scenarios
        registration: {
          clientId: aadClientId
          // clientSecretSettingName: aadAppRegistrationClientSecret_SecretName
          openIdIssuer: '${environment().authentication.loginEndpoint}/${tenant().tenantId}'
        }
        validation: {
           allowedAudiences: []
        }
      }
    }
    login: {
      preserveUrlFragmentsForLogins: false
    }
  }  
}
