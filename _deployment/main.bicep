param resourceGroupName string
param storageAccountName string
param storageSku string = 'Standard_LRS'

param useExistingKeyVault bool = false
param keyVaultName string
param keyVaultResourceGroupName string = resourceGroupName
param keyVaultSecretsReadersGroupObjectId string
param keyVaultSecretsContributorsGroupObjectId string

param location string = deployment().location
param tenancyAppName string
param tenancyStorageSecretName string

param aadDeploymentManagedIdentityName string
param aadDeploymentManagedIdentityResourceGroupName string
param aadDeploymentManagedIdentitySubscriptionId string = subscription().subscriptionId

param appConfigurationStoreName string
param appConfigurationStoreResourceGroupName string
param appConfigurationStoreSubscription string = subscription().subscriptionId
param appConfigurationLabel string

// param hostingPlatformSubscriptionId string = subscription().subscriptionId
param hostingPlatformResourceGroupName string
param hostingPlatformName string
@allowed([
  'ContainerApps'
  'Functions'
])
param hostingEnvironmentType string
param useExistingHosting bool = false

param appInsightsInstrumentationKeySecretName string = 'AppInsightsInstrumentationKey'

// param useAzureContainerRegistry bool = true
// param acrName string = ''
// param acrResourceGroupName string = ''
// param acrSubscriptionId string = subscription().subscriptionId

// param containerRegistryServer string = ''
// param containerRegistryUser string = ''
// @secure()
// param containerRegistryKey string = ''

// param tenancyContainerName string
// param tenancyContainerTag string

// param tenancyAadAppName string
// param enableAuth bool

// param tenancySpName string = tenancyAppName
// param tenancySpCredentialSecretName string
param tenancyAdminSpName string
param tenancyAdminSpCredentialSecretName string


param tenantId string
param resourceTags object = {}


targetScope = 'subscription'


// var useContainerApps = hostingPlatformType == 'ContainerApps'
var useFunctions = hostingEnvironmentType == 'Functions'


resource app_config 'Microsoft.AppConfiguration/configurationStores@2020-06-01' existing = {
  name: appConfigurationStoreName
  scope: resourceGroup(appConfigurationStoreSubscription, appConfigurationStoreResourceGroupName)
}

module tenancy_rg 'br:endjintestacr.azurecr.io/bicep/modules/resource_group:0.1.0-initial-modules-and-build.33' = {
  name: 'tenancyRg'
  params: {
    location: location
    name: resourceGroupName
    resourceTags: resourceTags
  }
}

module keyvault_rg 'br:endjintestacr.azurecr.io/bicep/modules/resource_group:0.1.0-initial-modules-and-build.33' = {
  name: 'keyVaultRg'
  params: {
    location: location
    name: keyVaultResourceGroupName
    resourceTags: resourceTags
  }
}

// module key_vault 'br:endjintestacr.azurecr.io/bicep/modules/key_vault_with_secrets_access_via_groups:0.1.0-initial-modules-and-build.33' = {
module key_vault '../../../Endjin.RecommendedPractices.Bicep/modules/public/key_vault_with_secrets_access_via_groups.bicep' = {
  scope: resourceGroup(keyVaultResourceGroupName)
  name: 'keyVaultWithGroupsAccess'
  params: {
    name: keyVaultName
    // useExisting: ???
    enableDiagnostics: true
    secretsReadersGroupObjectId: keyVaultSecretsReadersGroupObjectId
    secretsContributorsGroupObjectId: keyVaultSecretsContributorsGroupObjectId
    enabledForTemplateDeployment: true
    tenantId: tenantId
    location: location
    resourceTags: resourceTags
  }
  dependsOn: [
    keyvault_rg
  ]
}

module tenancy_storage 'br:endjintestacr.azurecr.io/bicep/modules/storage_account:0.1.0-initial-modules-and-build.33' = {
  scope: resourceGroup(resourceGroupName)
  name: storageAccountName
  params: {
    name: storageAccountName
    sku: storageSku
    saveAccessKeyToKeyVault: true
    keyVaultResourceGroupName: keyVaultResourceGroupName
    keyVaultName: key_vault.outputs.name
    keyVaultSecretName: tenancyStorageSecretName
    resource_tags: resourceTags
  }
  dependsOn: [
    tenancy_rg
  ]
}

module tenant_admin_service_principal 'modules/aad_service_principal_script.bicep' = {
  scope: resourceGroup(resourceGroupName)
  name: 'adminSpDeploy'
  params: {
    displayName: tenancyAdminSpName
    keyVaultName: keyVaultName
    keyVaultSecretName: tenancyAdminSpCredentialSecretName
    managedIdentityResourceId: aad_managed_id.id
    location: location
    resourceTags: resourceTags
  }
  dependsOn: [
    tenancy_rg
  ]
}

// module tenancy_service_container_app 'tenancy-aca.bicep' = if (useContainerApps) {
//   scope: tenancy_rg
//   name: 'tenancyInContainerApps'
//   params: {
//     appConfigurationLabel: appConfigurationLabel
//     appConfigurationStoreName: appConfigurationStoreName
//     appConfigurationStoreResourceGroupName: appConfigurationStoreResourceGroupName
//     appEnvironmentName: hostingPlatformName
//     appEnvironmentResourceGroupName: hostingPlatformResourceGroupName
//     appEnvironmentSubscription: hostingPlatformSubscriptionId
//     keyVaultName: keyVaultName
//     keyVaultResourceGroupName: useExistingKeyVault ? keyVaultResourceGroupName : tenancy_rg.outputs.name
//     tenancyAppName: tenancyAppName
//     tenancyStorageAccountName: tenancy_storage.outputs.name
//     tenancyContainerName: tenancyContainerName
//     tenancyContainerTag: tenancyContainerTag
//     tenancySpName: tenancySpName
//     tenancySpCredentialSecretName: tenancySpCredentialSecretName
//     tenancyStorageSecretName: tenancyStorageSecretName
//     useAzureContainerRegistry: useAzureContainerRegistry
//     acrName: acrName
//     acrResourceGroupName: acrResourceGroupName
//     acrSubscriptionId: acrSubscriptionId
//     containerRegistryServer: containerRegistryServer
//     containerRegistryUser: containerRegistryUser
//     containerRegistryKey: containerRegistryKey
//     aadManagedIdentityResourceId: aad_managed_id.id
//     location: location
//     resourceTags: resourceTags
//   }
// }

module tenancy_service_functions_app 'modules/tenancy-functions.bicep' = if (useFunctions) {
  scope: resourceGroup(resourceGroupName)
  name: 'tenancyInFunctions' 
  params: {
    appConfigurationLabel: appConfigurationLabel
    appConfigurationStoreName: appConfigurationStoreName
    appConfigurationStoreResourceGroupName: appConfigurationStoreResourceGroupName
    hostingEnvironmentName: hostingPlatformName
    useExistingHostingEnvironment: useExistingHosting
    hostingEnvironmentResourceGroupName: hostingPlatformResourceGroupName
    keyVaultName: keyVaultName
    keyVaultResourceGroupName: useExistingKeyVault ? keyVaultResourceGroupName : tenancy_rg.outputs.name
    tenancyAppName: tenancyAppName
    storageName: tenancy_storage.outputs.name
    storageSecretName: tenancyStorageSecretName
    appInsightsInstrumentationKeySecretName: appInsightsInstrumentationKeySecretName
    location: location
    resourceTags: resourceTags
  }
  dependsOn: [
    tenancy_rg
    keyvault_rg
  ]
}

resource aad_managed_id 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' existing = {
  name: aadDeploymentManagedIdentityName
  scope: resourceGroup(aadDeploymentManagedIdentitySubscriptionId, aadDeploymentManagedIdentityResourceGroupName)
}

// module tenancy_aad_app '../../erp/bicep/aad_app_deployment_script.bicep' = {
//   scope: tenancy_rg
//   name: 'aadAppScriptDeploy'
//   params: {
//     displayName: tenancyAadAppName
//     replyUrl: 'https://${useContainerApps ? tenancy_service_container_app.outputs.service_url : tenancy_service_functions_app.outputs.service_url}/.auth/login/aad/callback'
//     managedIdentityResourceId: aad_managed_id.id
//     resourceTags: resourceTags
//   }
// }

// Enable EasyAuth on the Tenancy service
// module tenancy_function_auth '../../erp/bicep/function-app-auth.bicep' = if (!useContainerApps) {
//   scope: resourceGroup(hostingPlatformResourceGroupName)
//   name: 'tenancyFunctionAppAuth'
//   params: {
//     authEnabled: enableAuth
//     aadClientId: tenancy_aad_app.outputs.application_id
//     functionName: tenancy_service_functions_app.outputs.name
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

// module init_tenancy '../../erp/bicep/init_marain_tenancy_deployment_script.bicep' = {
//   scope: tenancy_rg
//   name: 'initTenancyDeploy'
//   params: {
//     managedIdentityResourceId: aad_managed_id.id
//     servicePrincipalCredential: existing_key_vault.getSecret(tenancyAdminSpCredentialSecretName)
//     tenencyServiceAppId: tenancy_aad_app.outputs.application_id
//     tenencyServiceUri: 'https://${useContainerApps ? tenancy_service_container_app.outputs.service_url : tenancy_service_functions_app.outputs.service_url}/'
//     resourceTags: resourceTags
//   }
// }


output service_url string = tenancy_service_functions_app.outputs.service_url
// output service_url string = useContainerApps ? tenancy_service_container_app.outputs.service_url : tenancy_service_functions_app.outputs.service_url
// output fedemo_url string = tenancy_demofrontend.outputs.fqdn
// output sp_application_id string = useContainerApps ? tenancy_service_container_app.outputs.sp_application_id : tenancy_service_functions_app.outputs.sp_application_id
// output sp_object_id string = useContainerApps ? tenancy_service_container_app.outputs.sp_object_id : ''
// output aad_application_id string = tenancy_aad_app.outputs.application_id
// output aad_object_id string = tenancy_aad_app.outputs.object_id
