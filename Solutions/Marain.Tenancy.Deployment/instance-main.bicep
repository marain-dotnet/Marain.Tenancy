param resourceGroupName string
param appEnvironmentName string
param useExistingAppEnvironment bool = false
param appEnvironmentResourceGroupName string = resourceGroupName
param appEnvironmentSubscription string = subscription().subscriptionId
param keyVaultName string

param useExistingAppConfigurationStore bool = false
param appConfigurationStoreName string
param appConfigurationStoreResourceGroupName string = resourceGroupName
param appConfigurationStoreSubscription string = subscription().subscriptionId
param appConfigurationLabel string

param location string = deployment().location
param includeAcr bool = false
param acrName string = '${appEnvironmentName}acr'
param acrSku string = 'Standard'
param tenantId string
param resourceTags object = {}

targetScope = 'subscription'

resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: resourceTags
}

// Use existing App Environment
resource existing_app_environment 'Microsoft.Web/kubeEnvironments@2021-02-01' existing = if (useExistingAppEnvironment) {
  name: appEnvironmentName
  scope: resourceGroup(appEnvironmentSubscription, appEnvironmentResourceGroupName)
}
// Provision new App environment
module app_environment '../../erp/bicep/container_app_environment.bicep' = if (!useExistingAppEnvironment) {
  scope: rg
  name: 'appEnvDeploy'
  params: {
    environmentName: appEnvironmentName
    appinsightsName: '${appEnvironmentName}ai'
    workspaceName: '${appEnvironmentName}la'
    location: location
    createContainerRegistry: includeAcr
    containerRegistryName: acrName
    containerRegistrySku: acrSku
    resourceTags: resourceTags
  }
}

module key_vault '../../erp/bicep/key_vault.bicep' = {
  scope: rg
  name: 'keyVaultDeploy'
  params: {
    name: keyVaultName
    enable_diagnostics: true
    // TODO: Extract to config
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
      // Graph deploy managed identity
      {
        objectId: '9074d238-3f1c-4e39-9a56-bf71f54907b8'
        tenantId: tenantId
        permissions: {
          secrets: [
            'get'
            'set'
          ]
        }
      }
      {
        // Tenancy service SP ('jd' env)
        objectId: '454c822c-fa89-4a1c-9fbf-ab7203a9b5c6'
        tenantId: tenantId
        permissions: {
          secrets: [
            'get'
          ]
        }
      }
    ]
    enabledForTemplateDeployment: true
    tenantId: tenantId
    resource_tags: resourceTags
  }
}

resource existing_app_config 'Microsoft.AppConfiguration/configurationStores@2021-03-01-preview' existing = if (useExistingAppConfigurationStore) {
  name: appConfigurationStoreName
  scope: resourceGroup(appConfigurationStoreSubscription, appConfigurationStoreResourceGroupName)
}

// TODO: When referencing the main RG, this causes a conflicts (despite the conditions)
// resource app_config_rg 'Microsoft.Resources/resourceGroups@2021-04-01' = if (!useExistingAppConfigurationStore && appConfigurationStoreResourceGroupName != resourceGroupName) {
//   name: appConfigurationStoreResourceGroupName
//   location: location
// }

module app_config '../../erp/bicep/app_configuration.bicep' = if (!useExistingAppConfigurationStore) {
  name: 'appConfigDeploy'
  scope: resourceGroup(appConfigurationStoreResourceGroupName)
  params: {
    name: appConfigurationStoreName
    location: location
  }
}

module kubeenv_app_config_key '../../erp/bicep/set_app_configuration_keys.bicep' = {
  scope: resourceGroup(appConfigurationStoreSubscription, appConfigurationStoreResourceGroupName)
  name: 'kubeenvAppConfigKeyDeploy'
  params: {
    appConfigStoreName: appConfigurationStoreName
    label: appConfigurationLabel
    entries: [
      {
        name: 'ContainerAppEnvironmentResourceId'
        value: useExistingAppEnvironment ? existing_app_environment.id : app_environment.outputs.id
      }
    ]
  }
}

module aikey_secret '../../erp/bicep/key_vault_secret.bicep' = if (!useExistingAppEnvironment) {
  name: 'aiKeySecretDeploy'
  scope: rg
  params: {
    keyVaultName: keyVaultName
    secretName: 'AppInsightsInstrumentationKey'
    contentValue: app_environment.outputs.appinsights_instrumentation_key
    contentType: 'text/plain'
  }
}
