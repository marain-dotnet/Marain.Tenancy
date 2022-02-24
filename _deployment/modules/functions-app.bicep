@minLength(1)
param functionsAppName string
// @description('WebDeploy package location. This path is relative to the artifactsLocation parameter')
// param functionsAppPackageFolder string = ''
// @description('Name of the webdeploy package')
// param functionsAppPackageFileName string = ''

param hostingPlanResourceId string
// param hostingPlanSkuName string = 'Y1'
// param hostingPlanSkuTier string = 'Dynamic'
// param useExistingHostingPlan bool = false
// param existingHostingPlanResourceGroupName string = ''

param functionsAppPackageUri string = 'https://github.com/marain-dotnet/Marain.Tenancy/releases/download/1.1.14/Marain.Tenancy.Host.Functions.zip'

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

// param functionAadAppClientId string = ''

// @description('Enable EasyAuth for AAD using the specified Client ID/App ID')
// param easyAuthAadClientId string = ''
// param artifactsLocation string

// @secure()
// param artifactsLocationSasToken string

// param appSettings object = {}

param resourceTags object = {}

var storageAccountName = substring('${functionsAppName}func${uniqueString(resourceGroup().id)}', 0, 24)
// var storageAccountid = '${resourceGroup().id}/providers/Microsoft.Storage/storageAccounts/${storageAccountName_var}'
// var packageUri = '${artifactsLocation}/${functionsAppPackageFolder}/${functionsAppPackageFileName}${artifactsLocationSasToken}'


targetScope = 'resourceGroup'


module storage 'br:endjintestacr.azurecr.io/bicep/modules/storage_account:0.1.0-initial-modules-and-build.33' = {
  name: '${functionsAppName}FunctionAppStorage'
  params: {
    name: storageAccountName
    sku: storageAccountType
    saveAccessKeyToKeyVault: true
    keyVaultName: keyVaultName
    keyVaultResourceGroupName: keyVaultResourceGroupName
    keyVaultSecretName: functionStorageKeySecretName
    resource_tags: resourceTags
  }
}

module function_app '_functions_app.bicep' = {
  name: '${functionsAppName}FunctionAppInstance'
  params: {
    name: functionsAppName
    location: location
    hostingPlanResourceId: hostingPlanResourceId
    packageUri: functionsAppPackageUri
    resourceTags: resourceTags
  }
}


// output storageAccountConnectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName_var};AccountKey=${listKeys(storageAccountid, '2015-05-01-preview').key1}'
output servicePrincipalId string = function_app.outputs.servicePrincipalId
output fqdn string = function_app.outputs.fqdn
output id string = function_app.outputs.id
output name string = function_app.outputs.name
