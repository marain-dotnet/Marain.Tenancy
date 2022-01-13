param resourceGroupName string
param storageAccountName string
param storageSku string = 'Standard_LRS'
param keyVaultName string
param existingKeyVaultResourceGroupName string = ''
param useExistingKeyVault bool
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

param appEnvironmentSubscription string = subscription().subscriptionId
param appEnvironmentResourceGroupName string
param appEnvironmentName string

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
param tenancySpCredentialSecretName string
param tenantId string
param resourceTags object = {}

targetScope = 'subscription'

resource app_config 'Microsoft.AppConfiguration/configurationStores@2020-06-01' existing = {
  name: appConfigurationStoreName
  scope: resourceGroup(appConfigurationSubscription, appConfigurationStoreResourceGroupName)
}

// Use existing App Environment
resource kube_environment_rg 'Microsoft.Resources/resourceGroups@2021-04-01' existing = {
  name: appEnvironmentResourceGroupName
  scope: subscription(appEnvironmentSubscription)
}
resource kube_environment 'Microsoft.Web/kubeEnvironments@2021-02-01' existing = {
  name: appEnvironmentName
  scope: kube_environment_rg
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
    access_policies: [
      {
        // JamesD
        objectId: 'f3498fd9-cff0-44a9-991c-c017f481adf0'
        tenantId: tenantId
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
      {
        // Managed identity
        objectId: aad_managed_id.id
        tenantId: tenantId
        permissions: {
          secrets: [
            'get'
            'set'
          ]
        }
      }
    ]
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

module tenancy_app_service_principal '../../erp/bicep/aad_service_principal_script.bicep' = {
  scope: tenancy_rg
  name: 'spDeploy'
  params: {
    displayName: tenancyAppName
    keyVaultName: keyVaultName
    keyVaultSecretName: tenancySpCredentialSecretName
    managedIdentityResourceId: aad_managed_id.id
    resourceTags: resourceTags
  }
}

module tenancy_service 'tenancy-container-app.bicep' = {
  name: 'tenancyAppDeploy'
  scope: tenancy_rg
  dependsOn: [
    tenancy_app_service_principal
  ]
  params: {
    appName: tenancyAppName
    imageName: tenancyContainerName
    imageTag: tenancyContainerTag
    kubeEnvironmentId: kube_environment.id
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
    keyVaultName: keyVaultName
    storageName: tenancy_storage.outputs.name
    storageSecretName: tenancyStorageSecretName
  }
}

module tenency_uri_app_config_key '../../erp/bicep/app_configuration_keys.bicep' = {
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

resource aad_managed_id 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' existing = {
  name: aadDeploymentManagedIdentityName
  scope: resourceGroup(aadDeploymentManagedIdentitySubscriptionId, aadDeploymentManagedIdentityResourceGroupName)
}

// module aad_app '../../erp/bicep/aad_app_deployment_script.bicep' = {
//   scope: tenancy_rg
//   name: 'aadAppScriptDeploy'
//   params: {
//     displayName: tenancyAadAppName
//     managedIdentityResourceId: aad_managed_id.id
//   }
// }

output service_url string = tenancy_service.outputs.fqdn
output sp_application_id string = tenancy_app_service_principal.outputs.app_id
output sp_object_id string = tenancy_app_service_principal.outputs.object_id
// output aad_application_id string = aad_app.outputs.application_id
// output aad_object_id string = aad_app.outputs.object_id
