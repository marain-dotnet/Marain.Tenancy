// param resourceGroupName string
// param storageAccountName string
// param storageSku string = 'Standard_LRS'
param keyVaultName string
param keyVaultResourceGroupName string
// param useExistingKeyVault bool
// param location string = deployment().location
param tenancyAppName string

param hostingEnvironmentName string
param useExistingHostingEnvironment bool = false
param hostingEnvironmentResourceGroupName string = resourceGroup().name

param location string = resourceGroup().location

// param aadDeploymentManagedIdentityName string
// param aadDeploymentManagedIdentityResourceGroupName string
// param aadDeploymentManagedIdentitySubscriptionId string = subscription().subscriptionId

param appConfigurationStoreName string
param appConfigurationStoreResourceGroupName string
param appConfigurationSubscription string = subscription().subscriptionId
param appConfigurationLabel string

// param tenancyAadAppName string = ''

// param tenancyAdminSpName string
// param tenancyAdminSpCredentialSecretName string
// param tenantId string

// service config-related settings
param storageName string
param storageSecretName string
// @secure()
// param tenancyStorageConnectionString string
// @secure()
// param servicePrincipalCredential string

param appInsightsInstrumentationKeySecretName string

param resourceTags object = {}


targetScope = 'resourceGroup'


// var spClientId = json(servicePrincipalCredential).appId
// var spClientSecret = json(servicePrincipalCredential).password
// var spTenantId = json(servicePrincipalCredential).tenant
// var azureServicesAuthConnectionString = ''

// resource app_config 'Microsoft.AppConfiguration/configurationStores@2020-06-01' existing = {
//   name: appConfigurationStoreName
//   scope: resourceGroup(appConfigurationSubscription, appConfigurationStoreResourceGroupName)
// }


// Retrieve/provision the target AppService plan
module hosting_plan 'br:endjintestacr.azurecr.io/bicep/modules/app_service_plan:0.1.0-initial-modules-and-build.33' = {
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
module storage_secret 'br:endjintestacr.azurecr.io/bicep/modules/key_vault_secret:0.1.0-initial-modules-and-build.33' = {
  name: 'getStorageSecret'
  scope: resourceGroup(keyVaultResourceGroupName)
  params: {
    keyVaultName: keyVaultName
    secretName: storageSecretName
    useExisting: true
  }
}

// Deploy Tenancy as a Function App
module tenancy_service 'functions-app.bicep' = {
  name: 'tenancyFunctionApp'
  params: {
    functionsAppName: tenancyAppName
    hostingPlanResourceId: hosting_plan.outputs.id
    keyVaultName: keyVaultName
    keyVaultResourceGroupName: keyVaultResourceGroupName
    functionStorageKeySecretName: 'tenancy-function-storage-key'
    location: location
    resourceTags: resourceTags
  }
}

// Configure the Tenancy app settings
module tenancy_app_settings 'function-app-settings.bicep' = {
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

// Enable EasyAuth on the Tenancy service
// module tenancy_app_auth '../../erp/bicep/function-app-auth.bicep' = if (!empty(tenancyAadAppName)) {
//   name: 'tenancyFunctionAppAuth'
//   params: {
//     aadClientId: tenancyAadAppName
//     functionName: tenancy_service.outputs.name
//     unauthenticatedClientAction: 'RedirectToLoginPage'
//   }
// }

module tenancy_uri_app_config_key 'br:endjintestacr.azurecr.io/bicep/modules/set_app_configuration_keys:0.1.0-initial-modules-and-build.33' = {
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

// resource aad_managed_id 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' existing = {
//   name: aadDeploymentManagedIdentityName
//   scope: resourceGroup(aadDeploymentManagedIdentitySubscriptionId, aadDeploymentManagedIdentityResourceGroupName)
// }

// module tenancy_aad_app '../../erp/bicep/aad_app_deployment_script.bicep' = {
//   scope: tenancy_rg
//   name: 'aadAppScriptDeploy'
//   params: {
//     displayName: tenancyAadAppName
//     replyUrl: 'https://${tenancy_service.outputs.fqdn}/.auth/login/aad/callback'
//     managedIdentityResourceId: aad_managed_id.id
//   }
// }

// module tenancy_aad_app_id_config_key '../../erp/bicep/set_app_configuration_keys.bicep' = {
//   scope: resourceGroup(appConfigurationSubscription, appConfigurationStoreResourceGroupName)
//   name: 'tenncyAadAppIdConfigKeyDeploy'
//   params: {
//     appConfigStoreName: appConfigurationStoreName
//     label: appConfigurationLabel
//     entries: [
//       {
//         name: 'TenancyAadAppId'
//         value: tenancy_aad_app.outputs.application_id
//       }
//     ]
//   }
// }


output service_url string = tenancy_service.outputs.fqdn
output sp_application_id string = tenancy_service.outputs.servicePrincipalId
// output sp_object_id string = tenancy_app_service_principal.outputs.object_id
// output aad_application_id string = tenancy_aad_app.outputs.application_id
// output aad_object_id string = tenancy_aad_app.outputs.object_id
output name string = tenancy_service.outputs.name
