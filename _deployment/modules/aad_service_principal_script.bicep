param displayName string
param keyVaultName string
param keyVaultSecretName string
param managedIdentityResourceId string
param location string = resourceGroup().location
param resourceTags object

targetScope = 'resourceGroup'

var deployScriptResourceName = 'aad-sp-deployscript-${displayName}'
var scriptFilePath = 'deploymentScripts/deploy-aad-service-principal.ps1'
var scriptContent = loadTextContent(scriptFilePath)

resource aad_service_principal_deploy_script 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: deployScriptResourceName
  location: location
  kind: 'AzurePowerShell'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityResourceId}': {}
    }
  }
  properties: {
    arguments: '-DisplayName "${displayName}" -KeyVaultName "${keyVaultName}" -KeyVaultSecretName "${keyVaultSecretName}"'
    azPowerShellVersion: '7.3'
    cleanupPreference: 'OnSuccess'
    retentionInterval: 'P1D'
    scriptContent: scriptContent
    timeout: 'PT30M'
  }
  tags: resourceTags
}

output app_id string = aad_service_principal_deploy_script.properties.outputs.appId
output object_id string = aad_service_principal_deploy_script.properties.outputs.objectId
