param keyVaultName string
param keyVaultResourceGroupName string
param tenancyAppName string
param functionStorageAccountName string = ''
param tenancyAppVersion string = '1.1.14'

param hostingEnvironmentName string
param useExistingHostingEnvironment bool = false
param hostingEnvironmentResourceGroupName string = resourceGroup().name

param location string = resourceGroup().location

param appConfigurationStoreName string
param appConfigurationStoreResourceGroupName string
param appConfigurationSubscription string = subscription().subscriptionId
param appConfigurationLabel string

// service config-related settings
param storageName string
param storageSecretName string
param appInsightsInstrumentationKeySecretName string

param resourceTags object = {}


targetScope = 'resourceGroup'


var tenancyPackageUri = 'https://github.com/marain-dotnet/Marain.Tenancy/releases/download/${tenancyAppVersion}/Marain.Tenancy.Host.Functions.zip'


// Retrieve/provision the target AppService plan
module hosting_plan 'br:endjin.azurecr.io/bicep/general/app-service-plan:0.1.1' = {
  name: 'tenancyHostingPlan'
  scope:resourceGroup(hostingEnvironmentResourceGroupName)
  params: {
    name: hostingEnvironmentName
    useExisting: useExistingHostingEnvironment
    existingPlanResourceGroupName: hostingEnvironmentResourceGroupName
    skuName: 'Y1'
    skuTier: 'Dynamic'
    location: location
  }
}

// Get a reference to the key vault so we can use the getSecrets() function
resource key_vault 'Microsoft.KeyVault/vaults@2021-06-01-preview' existing = {
  name: keyVaultName
  scope: resourceGroup(keyVaultResourceGroupName)
}

// Lookup the storage secret as we'll use its URI later in app settings
module storage_secret 'br:endjin.azurecr.io/bicep/general/key-vault-secret:1.0.1' = {
  name: 'getStorageSecret'
  scope: resourceGroup(keyVaultResourceGroupName)
  params: {
    keyVaultName: keyVaultName
    secretName: storageSecretName
    useExisting: true
  }
}

// Deploy Tenancy as a Function App
module tenancy_service './functions-app.bicep' = {
  name: 'tenancyFunctionApp'
  params: {
    functionsAppName: tenancyAppName
    functionsAppPackageUri: tenancyPackageUri
    functionStorageAccountName: functionStorageAccountName
    hostingPlanResourceId: hosting_plan.outputs.id
    keyVaultName: keyVaultName
    keyVaultResourceGroupName: keyVaultResourceGroupName
    functionStorageKeySecretName: 'tenancy-function-storage-key'
    location: location
    resourceTags: resourceTags
  }
}

// Configure the Tenancy app settings
module tenancy_app_settings './function-app-settings.bicep' = {
  name: 'tenancyFunctionAppSettings'
  params: {
    functionName: tenancy_service.outputs.name
    storageAccountKey: key_vault.getSecret(storageSecretName)
    storageAccountName: storageName
    appInsightsInstrumentationKey: key_vault.getSecret(appInsightsInstrumentationKeySecretName)
    appSettings: {
      AzureServicesAuthConnectionString: ''     
      'RootTenantBlobStorageConfigurationOptions:AccountName': storageName
      'RootTenantBlobStorageConfigurationOptions:KeyVaultName': keyVaultName
      'RootTenantBlobStorageConfigurationOptions:AccountKeySecretName': storageSecretName
      SubscriptionId: subscription().subscriptionId
      'TenantCacheConfiguration:GetTenantResponseCacheControlHeaderValue': 'max-age=300'
      'TenantCloudBlobContainerFactoryOptions:AzureServicesAuthConnectionString': ''
      TenantId: subscription().tenantId
      // new style config
      // 'ServiceIdentity:IdentitySourceType': 'AzureIdentityDefaultAzureCredential'
      // 'RootTenantBlobStorageConfiguration:AccessKeyInKeyVault:VaultName': keyVaultName
      // 'RootTenantBlobStorageConfiguration:AccessKeyInKeyVault:SecretName': storageSecretName
    }
  }
}

module tenancy_uri_app_config_key 'br:endjin.azurecr.io/bicep/general/set-app-configuration-keys:1.0.1' = {
  scope: resourceGroup(appConfigurationSubscription, appConfigurationStoreResourceGroupName)
  name: 'tenncyUriAppConfigKeyDeploy'
  params: {
    appConfigStoreName: appConfigurationStoreName
    label: appConfigurationLabel
    entries: [
      {
        name: 'TenancyServiceUrl'
        value: 'https://${tenancy_service.outputs.fqdn}'
      }
    ]
  }
}


output fqdn string = tenancy_service.outputs.fqdn
output managedIdentityAppId string = tenancy_service.outputs.servicePrincipalId
output name string = tenancy_service.outputs.name
