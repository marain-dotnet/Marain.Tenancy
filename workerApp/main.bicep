param tenancyResourceGroupName string
param storageAccountName string = '${appEnvironmentName}sa'
param keyVaultName string = '${appEnvironmentName}kv'
param location string
// param tenancyAppName string

param useExistingAppEnvironment bool = false
param appEnvironmentResourceGroupName string
param appEnvironmentName string
param appEnvironmentLocation string = location
// param acrName string = '${appEnvironmentName}acr'
// param acrKey string

targetScope = 'subscription'

// Use existing App Environment
resource existing_app_env_rg 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (useExistingAppEnvironment) {
  name: appEnvironmentResourceGroupName
}
resource existing_app_environment 'Microsoft.Web/kubeEnvironments@2021-02-01' existing = if (useExistingAppEnvironment) {
  name: appEnvironmentName
  scope: app_env_rg
}

// Provision new App environment
resource app_env_rg 'Microsoft.Resources/resourceGroups@2021-04-01' = if (!useExistingAppEnvironment) {
  name: appEnvironmentResourceGroupName
  location: location
}
module app_environment '../erp/bicep/container_app_environment.bicep' = if (!useExistingAppEnvironment) {
  scope: app_env_rg
  name: 'appEnvDeploy'
  params: {
    environment_name: appEnvironmentName
    appinsights_name: '${appEnvironmentName}ai'
    workspace_name: '${appEnvironmentName}la'
    location: location
    include_container_registry: true
    enable_container_registry_adminuser: true
  }
}

// Provision Tenancy service
resource tenancy_rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: tenancyResourceGroupName
  location: location
}

module tenancy_Storage '../erp/bicep/storage_account.bicep' = {
  scope: tenancy_rg
  name: storageAccountName
  params: {
    name: storageAccountName
    sku: 'Standard_LRS'
  }
}

module key_vault '../erp/bicep/key_vault.bicep' = {
  scope: tenancy_rg
  name: 'keyVaultDeploy'
  params: {
    name: keyVaultName
    access_policies: [
      {
        // JamesD
        objectId: 'f3498fd9-cff0-44a9-991c-c017f481adf0'
        tenantId: '0f621c67-98a0-4ed5-b5bd-31a35be41e29'
        permissions: {
          secrets: [
            'all'
          ]
        }
      }
      {
        // SP
        objectId: '8ec5ab5b-89e6-4afb-90de-7a502574e9fa'
        tenantId: '0f621c67-98a0-4ed5-b5bd-31a35be41e29'
        permissions: {
          secrets: [
            'get'
          ]
        }
      }
    ]
  }
}

// resource kubeEnvironment 'Microsoft.Web/kubeEnvironments@2021-01-15' existing = {
//   name: appEnvironmentName
// }

// module tenancy 'modules/containerApp.bicep' = {
//   name: 'tenancyDeploy'
//   params: {
//     location: containerAppLocation
//     containerImage: 'endmarainspike.azurecr.io/marain/tenancy-service:latest'
//     daprComponents: {}
//     daprSecrets: {}
//     ingressIsExternal: true
//     ingressTargetPort: 80
//     kubeEnvironmentId: kubeEnvironment.id
//     name: tenancyAppName
//     includeDapr: false
//     environmentVariables: [
//       {
//         name: 'TenantCloudBlobContainerFactoryOptions__AzureServicesAuthConnectionString'
//         value: ''
//       }
//       {
//         name: 'RootTenantBlobStorageConfigurationOptions__RootTenantBlobStorageConfiguration__KeyVaultName'
//         value: key_vault.name
//       }
//       {
//         name: 'RootTenantBlobStorageConfigurationOptions__RootTenantBlobStorageConfiguration__AccountKeySecretName'
//         value: 'storageKey'
//       }
//       {
//         name: 'RootTenantBlobStorageConfigurationOptions__AccountName'
//         value: tenancy_Storage.name
//       }
//     ]
//     acrKey: acrKey
//     acrName: acrName
//   }
// }
