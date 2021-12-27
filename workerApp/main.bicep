param storageAccountName string = '${appEnvironmentName}sa'
param keyVaultName string = '${appEnvironmentName}kv'
param acrName string = '${appEnvironmentName}acr'
param location string
// param tenancyAppName string
param appEnvironmentName string
// param containerAppLocation string
// param acrKey string

resource tenancy_Storage 'Microsoft.Storage/storageAccounts@2021-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2021-06-01-preview' = {
  name: acrName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    adminUserEnabled: true
  }
}

resource key_vault 'Microsoft.KeyVault/vaults@2021-06-01-preview' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: '0f621c67-98a0-4ed5-b5bd-31a35be41e29'
    accessPolicies: [
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
