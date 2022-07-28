@minLength(1)
param functionsAppName string
param hostingPlanResourceId string
param functionsAppPackageUri string
param functionStorageAccountName string = ''
@description('Storage Account type')
@allowed([
  'Standard_LRS'
  'Standard_GRS'
  'Standard_RAGRS'
])
param storageAccountType string = 'Standard_LRS'
param keyVaultName string
param keyVaultResourceGroupName string
param functionStorageKeySecretName string
param location string = resourceGroup().location
param resourceTags object = {}


var storageAccountName = empty(functionStorageAccountName) ? substring('${functionsAppName}func${uniqueString(resourceGroup().id)}', 0, 24) : functionStorageAccountName


targetScope = 'resourceGroup'


module storage 'br:endjin.azurecr.io/bicep/general/storage-account:1.0.2' = {
  name: '${functionsAppName}FunctionAppStorage'
  params: {
    name: storageAccountName
    sku: storageAccountType
    saveAccessKeyToKeyVault: true
    keyVaultName: keyVaultName
    keyVaultResourceGroupName: keyVaultResourceGroupName
    keyVaultAccessKeySecretName: functionStorageKeySecretName
    resource_tags: resourceTags
  }
}

module function_app './_functions_app.bicep' = {
  name: '${functionsAppName}FunctionAppInstance'
  params: {
    name: functionsAppName
    location: location
    hostingPlanResourceId: hostingPlanResourceId
    packageUri: functionsAppPackageUri
    resourceTags: resourceTags
  }
}


output servicePrincipalId string = function_app.outputs.servicePrincipalId
output fqdn string = function_app.outputs.fqdn
output id string = function_app.outputs.id
output name string = function_app.outputs.name
