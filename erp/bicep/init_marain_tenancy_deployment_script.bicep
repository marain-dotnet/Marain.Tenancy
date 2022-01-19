param tenencyServiceUri string
param tenencyServiceAppId string
@secure()
param servicePrincipalCredential string
param managedIdentityResourceId string
param location string = resourceGroup().location
param resourceTags object = {}

targetScope = 'resourceGroup'

var deployScriptResourceName = 'init-tenancy-deployscript'
var scriptFilePath = 'deploymentScripts/run-marain-cli.sh'
var scriptContent = loadTextContent(scriptFilePath)
var spClientId = json(servicePrincipalCredential).appId
var spClientSecret = json(servicePrincipalCredential).password
var spTenantId = json(servicePrincipalCredential).tenant

resource aad_deploy_script 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: deployScriptResourceName
  location: location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
  }
  properties: {
    arguments: 'init'
    environmentVariables: [
      {
        name: 'TenancyClient__TenancyServiceBaseUri'
        value: tenencyServiceUri
      }
      {
        name: 'TenancyClient__ResourceIdForMsiAuthentication'
        value: tenencyServiceAppId
      }
      {
        name: 'AzureServicesAuthConnectionString'
        secureValue: 'RunAs=App;AppId=${spClientId};TenantId=${spTenantId};AppKey=${spClientSecret}'
      }
    ]
    azCliVersion: '2.29.2'
    cleanupPreference: 'OnSuccess'
    retentionInterval: 'P1D'
    scriptContent: scriptContent
    timeout: 'PT30M'
  }
  tags: resourceTags
}
