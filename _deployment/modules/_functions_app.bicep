param name string
param hostingPlanResourceId string
param location string = resourceGroup().location
param packageUri string = ''
param resourceTags object = {}


targetScope = 'resourceGroup'


resource function_app 'Microsoft.Web/sites@2021-02-01' = {
  kind: 'functionapp'
  name: name
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlanResourceId
  }
  tags: resourceTags
}

resource function_app_msdeploy 'Microsoft.Web/sites/extensions@2021-02-01' = if ( !(empty(packageUri)) ) {
  parent: function_app
  name: 'MSDeploy'
  properties: {
    packageUri: packageUri
  }
}


output id string = function_app.id
output servicePrincipalId string = function_app.identity.principalId
output fqdn string = function_app.properties.defaultHostName
output name string = function_app.name
