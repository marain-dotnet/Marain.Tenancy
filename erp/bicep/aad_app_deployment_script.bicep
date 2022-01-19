param displayName string
param replyUrl string
param managedIdentityResourceId string
param location string = resourceGroup().location
param resourceTags object = {}
param timestamp string = utcNow()

targetScope = 'resourceGroup'

var deployScriptResourceName = 'aad-app-deployscript-${displayName}'
var scriptFilePath = 'deploymentScripts/deploy-aad-app.ps1'
var scriptContent = loadTextContent(scriptFilePath)

resource aad_deploy_script 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
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
    arguments: '-DisplayName \\"${displayName}\\" -ReplyUrls @(\\"${replyUrl}\\") -Verbose'
    azPowerShellVersion: '6.6'
    cleanupPreference: 'OnSuccess'
    forceUpdateTag: timestamp   // the script is idempotent, so it can always run
    retentionInterval: 'P1D'
    scriptContent: scriptContent
    timeout: 'PT30M'
  }
  tags: resourceTags
}

output application_id string = aad_deploy_script.properties.outputs.applicationId
output object_id string = aad_deploy_script.properties.outputs.objectId
