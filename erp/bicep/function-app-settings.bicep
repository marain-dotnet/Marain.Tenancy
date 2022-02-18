param functionName string
param extensionVersion string = '~3'
param workerRuntime string = 'dotnet'
param appSettings object
param storageAccountName string
@secure()
param storageAccountKey string


targetScope = 'resourceGroup'


var functionSettings = {
  // AzureWebJobsDashboard: '@Microsoft.KeyVault(SecretUri=${storage_secret.outputs.secretUriWithVersion})'
  // AzureWebJobsStorage: '@Microsoft.KeyVault(SecretUri=${storage_secret.outputs.secretUriWithVersion})'
  // WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${storage_secret.outputs.secretUriWithVersion})'
  // WEBSITE_CONTENTSHARE: tenancyAppName
  AzureConfigurationStorage: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccountKey}}'
  AzureWebJobsDashboard: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccountKey}'
  AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccountKey}}'
  FUNCTIONS_EXTENSION_VERSION: extensionVersion
  FUNCTIONS_WORKER_RUNTIME: workerRuntime
  WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccountKey}'
  WEBSITE_CONTENTSHARE: toLower(functionName)
}
var finalAppSettings = union(functionSettings,appSettings)


resource function_app 'Microsoft.Web/sites@2021-02-01' existing = {
  name: functionName
}

resource function_app_settings 'Microsoft.Web/sites/config@2021-02-01' = {
  parent: function_app
  name: 'appsettings'
  properties: finalAppSettings
}
