param functionName string
@allowed([
  'AllowAnonymous'
  'RedirectToLoginPage'
])
param unauthenticatedClientAction string = 'RedirectToLoginPage'
param aadClientId string

param issuer string = ''
param defaultProvider string = 'AzureActiveDirectory'
param authEnabled bool = true
param tokenStoreEnabled bool = true


targetScope = 'resourceGroup'


resource function_app 'Microsoft.Web/sites@2021-02-01' existing = {
  name: functionName
}

resource authSettings 'Microsoft.Web/sites/config@2021-02-01' = {
  parent: function_app
  name: 'authsettings'
  properties: {
    enabled: authEnabled
    unauthenticatedClientAction: unauthenticatedClientAction
    tokenStoreEnabled: tokenStoreEnabled
    defaultProvider: defaultProvider
    clientId: aadClientId
    issuer: issuer
  }
}
