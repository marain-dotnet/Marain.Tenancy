param resourceGroupName string
param storageAccountName string
param storageSku string = 'Standard_LRS'
param keyVaultName string
param existingKeyVaultResourceGroupName string = ''
param useExistingKeyVault bool
param keyVaultReaderGroupObjectId string = ''
param keyVaultContributorGroupObjectId string = ''
param location string = deployment().location
param tenancyAppName string
param tenancyStorageSecretName string

param aadDeploymentManagedIdentityName string
param aadDeploymentManagedIdentityResourceGroupName string
param aadDeploymentManagedIdentitySubscriptionId string = subscription().subscriptionId

param appConfigurationStoreName string
param appConfigurationStoreResourceGroupName string
param appConfigurationSubscription string = subscription().subscriptionId
param appConfigurationLabel string

param hostingPlatformSubscriptionId string = subscription().subscriptionId
param hostingPlatformResourceGroupName string
param hostingPlatformName string
@allowed([
  'container-apps'
  'functions'
])
param hostingPlatformType string
param useExistingHosting bool

param useAzureContainerRegistry bool = true
param acrName string = ''
param acrResourceGroupName string = ''
param acrSubscriptionId string = subscription().subscriptionId

param containerRegistryServer string = ''
param containerRegistryUser string = ''
@secure()
param containerRegistryKey string = ''

param tenancyContainerName string
param tenancyContainerTag string

param tenancyAadAppName string
param enableAuth bool

param tenancySpName string = tenancyAppName
param tenancySpCredentialSecretName string
param tenancyAdminSpName string
param tenancyAdminSpCredentialSecretName string
param tenantId string
param resourceTags object = {}

var useContainerApps = hostingPlatformType == 'container-apps'
var useFunctions = hostingPlatformType == 'functions'

targetScope = 'subscription'

resource app_config 'Microsoft.AppConfiguration/configurationStores@2020-06-01' existing = {
  name: appConfigurationStoreName
  scope: resourceGroup(appConfigurationSubscription, appConfigurationStoreResourceGroupName)
}

// Provision Tenancy resources
resource tenancy_rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: resourceTags
}

// key vault handling
resource existing_key_vault 'Microsoft.KeyVault/vaults@2021-06-01-preview' existing = if (useExistingKeyVault) {
  name: keyVaultName
  scope: resourceGroup(existingKeyVaultResourceGroupName)
}
module key_vault '../../erp/bicep/key_vault.bicep' = if (!useExistingKeyVault) {
  scope: tenancy_rg
  name: 'keyVaultDeploy'
  params: {
    name: keyVaultName
    enabledForTemplateDeployment: true
    enable_diagnostics: true
    access_policies: !empty(keyVaultReaderGroupObjectId) && !empty(keyVaultContributorGroupObjectId) ? [
      {
        objectId: keyVaultReaderGroupObjectId
        tenantId: tenantId
        permissions: {
          secrets: [
            'get'
          ]
        }
      }
      {
        objectId: keyVaultContributorGroupObjectId
        tenantId: tenantId
        permissions: {
          secrets: [
            'get'
            'set'
          ]
        }
      }
    ] : []
    tenantId: tenantId
    resource_tags: resourceTags
  }
}


module tenancy_storage '../../erp/bicep/storage_account.bicep' = {
  scope: tenancy_rg
  name: storageAccountName
  params: {
    name: storageAccountName
    sku: storageSku
    saveAccessKeyToKeyVault: true
    keyVaultResourceGroupName: useExistingKeyVault ? existingKeyVaultResourceGroupName : resourceGroupName
    keyVaultName: useExistingKeyVault ? existing_key_vault.name : key_vault.name
    keyVaultSecretName: tenancyStorageSecretName
    resource_tags: resourceTags
  }
}

module tenant_admin_service_principal '../../erp/bicep/aad_service_principal_script.bicep' = {
  scope: tenancy_rg
  name: 'adminSpDeploy'
  params: {
    displayName: tenancyAdminSpName
    keyVaultName: keyVaultName
    keyVaultSecretName: tenancyAdminSpCredentialSecretName
    managedIdentityResourceId: aad_managed_id.id
    resourceTags: resourceTags
  }
}

module tenancy_service_container_app 'tenancy-aca.bicep' = if (useContainerApps) {
  scope: tenancy_rg
  name: 'tenancyInContainerApps'
  params: {
    appConfigurationLabel: appConfigurationLabel
    appConfigurationStoreName: appConfigurationStoreName
    appConfigurationStoreResourceGroupName: appConfigurationStoreResourceGroupName
    appEnvironmentName: hostingPlatformName
    appEnvironmentResourceGroupName: hostingPlatformResourceGroupName
    appEnvironmentSubscription: hostingPlatformSubscriptionId
    keyVaultName: keyVaultName
    keyVaultResourceGroupName: useExistingKeyVault ? existingKeyVaultResourceGroupName : tenancy_rg.name
    tenancyAppName: tenancyAppName
    tenancyStorageAccountName: tenancy_storage.outputs.name
    tenancyContainerName: tenancyContainerName
    tenancyContainerTag: tenancyContainerTag
    tenancySpName: tenancySpName
    tenancySpCredentialSecretName: tenancySpCredentialSecretName
    tenancyStorageSecretName: tenancyStorageSecretName
    useAzureContainerRegistry: useAzureContainerRegistry
    acrName: acrName
    acrResourceGroupName: acrResourceGroupName
    acrSubscriptionId: acrSubscriptionId
    containerRegistryServer: containerRegistryServer
    containerRegistryUser: containerRegistryUser
    containerRegistryKey: containerRegistryKey
    aadManagedIdentityResourceId: aad_managed_id.id
    location: location
    resourceTags: resourceTags
  }
}

module tenancy_service_functions_app 'tenancy-functions.bicep' = if (useFunctions) {
  scope: tenancy_rg
  name: 'tenancyInFunctions' 
  params: {
    appConfigurationLabel: appConfigurationLabel
    appConfigurationStoreName: appConfigurationStoreName
    appConfigurationStoreResourceGroupName: appConfigurationStoreResourceGroupName
    hostingEnvironmentName: hostingPlatformName
    useExistingHostingEnvironment: useExistingHosting
    hostingEnvironmentResourceGroupName: hostingPlatformResourceGroupName
    keyVaultName: keyVaultName
    keyVaultResourceGroupName: useExistingKeyVault ? existingKeyVaultResourceGroupName : tenancy_rg.name
    tenancyAppName: tenancyAppName
    storageName: tenancy_storage.outputs.name
    storageSecretName: tenancyStorageSecretName
    appInsightsInstrumentationKey: existing_key_vault.getSecret('AppInsightsInstrumentationKey')
    resourceTags: resourceTags
  }
}

resource aad_managed_id 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' existing = {
  name: aadDeploymentManagedIdentityName
  scope: resourceGroup(aadDeploymentManagedIdentitySubscriptionId, aadDeploymentManagedIdentityResourceGroupName)
}

module tenancy_aad_app '../../erp/bicep/aad_app_deployment_script.bicep' = {
  scope: tenancy_rg
  name: 'aadAppScriptDeploy'
  params: {
    displayName: tenancyAadAppName
    replyUrl: 'https://${useContainerApps ? tenancy_service_container_app.outputs.service_url : tenancy_service_functions_app.outputs.service_url}/.auth/login/aad/callback'
    managedIdentityResourceId: aad_managed_id.id
    resourceTags: resourceTags
  }
}

// Enable EasyAuth on the Tenancy service
module tenancy_function_auth '../../erp/bicep/function-app-auth.bicep' = if (!useContainerApps) {
  scope: resourceGroup(hostingPlatformResourceGroupName)
  name: 'tenancyFunctionAppAuth'
  params: {
    authEnabled: enableAuth
    aadClientId: tenancy_aad_app.outputs.application_id
    functionName: tenancy_service_functions_app.outputs.name
  }
}

module tenancy_aad_app_id_config_key '../../erp/bicep/set_app_configuration_keys.bicep' = {
  scope: resourceGroup(appConfigurationSubscription, appConfigurationStoreResourceGroupName)
  name: 'tenncyAadAppIdConfigKeyDeploy'
  params: {
    appConfigStoreName: appConfigurationStoreName
    label: appConfigurationLabel
    entries: [
      {
        name: 'TenancyAadAppId'
        value: tenancy_aad_app.outputs.application_id
      }
    ]
  }
}

module init_tenancy '../../erp/bicep/init_marain_tenancy_deployment_script.bicep' = {
  scope: tenancy_rg
  name: 'initTenancyDeploy'
  params: {
    managedIdentityResourceId: aad_managed_id.id
    servicePrincipalCredential: existing_key_vault.getSecret(tenancyAdminSpCredentialSecretName)
    tenencyServiceAppId: tenancy_aad_app.outputs.application_id
    tenencyServiceUri: 'https://${useContainerApps ? tenancy_service_container_app.outputs.service_url : tenancy_service_functions_app.outputs.service_url}/'
    resourceTags: resourceTags
  }
}

//
// ACA handling for the DemoFrontEnd app
//

resource kube_environment 'Microsoft.Web/kubeEnvironments@2021-02-01' existing = {
  name: hostingPlatformName
  scope: resourceGroup(hostingPlatformSubscriptionId, hostingPlatformResourceGroupName)
}

module tenancy_demofrontend 'tenancy-fedemo-container-app.bicep' = if (hostingPlatformType == 'container-apps') {
  name: 'tenancyDemoFeAppDeploy'
  scope: tenancy_rg
  dependsOn: [
    tenancy_service_container_app
  ]
  params: {
    appName: 'tenancyfedemo'
    imageName: 'marain/tenancy-demofrontend'
    imageTag: tenancyContainerTag
    kubeEnvironmentId: kube_environment.id
    minReplicas: 1
    location: location
    resourceTags: resourceTags

    // container registry related settings
    useAzureContainerRegistry: useAzureContainerRegistry
    acrName: acrName
    acrResourceGroupName: acrResourceGroupName
    acrSubscription: acrSubscriptionId
    containerRegistryServer: containerRegistryServer
    containerRegistryUser: containerRegistryUser
    containerRegistryKey: containerRegistryKey

    // service config-related settings
    // TODO: Get a key vault reference when not using an existing key vault to enable the getSecret() call
    servicePrincipalCredential: existing_key_vault.getSecret(tenancySpCredentialSecretName)
    appInsightsInstrumentationKey: existing_key_vault.getSecret('AppInsightsInstrumentationKey')
  }
}

output service_url string = useContainerApps ? tenancy_service_container_app.outputs.service_url : tenancy_service_functions_app.outputs.service_url
output fedemo_url string = tenancy_demofrontend.outputs.fqdn
output sp_application_id string = useContainerApps ? tenancy_service_container_app.outputs.sp_application_id : tenancy_service_functions_app.outputs.sp_application_id
output sp_object_id string = useContainerApps ? tenancy_service_container_app.outputs.sp_object_id : ''
output aad_application_id string = tenancy_aad_app.outputs.application_id
output aad_object_id string = tenancy_aad_app.outputs.object_id
