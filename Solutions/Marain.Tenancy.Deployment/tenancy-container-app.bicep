param appName string
param location string
param kubeEnvironmentId string
param imageName string
param imageTag string
param resourceTags object = {}

// container registry related settings
param useAzureContainerRegistry bool = true
param acrName string = ''
param acrResourceGroupName string = ''
param acrSubscription string = subscription().subscriptionId
param containerRegistryServer string = ''
param containerRegistryUser string = ''
@secure()
param containerRegistryKey string = ''

// service config-related settings
param keyVaultName string
param storageName string
param storageSecretName string
@secure()
param servicePrincipalCredential string


targetScope = 'resourceGroup'


var containerImage = '${imageName}:${imageTag}'
// parse the keyvault secret into the its constituent parts
var spClientId = json(servicePrincipalCredential).appId
var spClientSecret = json(servicePrincipalCredential).password
var spTenantId = json(servicePrincipalCredential).tenant
// construct the connection string that will set as a secret
var azureServicesAuthConnectionString = 'RunAs%3DApp;AppId%3D${spClientId};TenantId%3D${spTenantId};AppKey%3D${spClientSecret}'
var azureServicesAuthConnectionStringSecretRef = 'azure-services-auth-connection-string'

resource acr 'Microsoft.ContainerRegistry/registries@2021-09-01' existing = if (useAzureContainerRegistry) {
  name: acrName
  scope: resourceGroup(acrSubscription, acrResourceGroupName)
}

module tenancy_service '../../erp/bicep/container_app.bicep' = {
  name: 'containerAppDeploy'
  params: {
    location: location
    containerImage: useAzureContainerRegistry ? '${acr.properties.loginServer}/${containerImage}' : '${containerRegistryServer}/${containerImage}'
    activeRevisionsMode: 'single'
    daprComponents: []
    secrets: [
      {
        name: azureServicesAuthConnectionStringSecretRef
        value: azureServicesAuthConnectionString
      }
    ]
    ingressIsExternal: true
    ingressTargetPort: 80
    kubeEnvironmentId: kubeEnvironmentId
    name: appName
    includeDapr: false
    environmentVariables: [
      {
        name: 'TenantCloudBlobContainerFactoryOptions__AzureServicesAuthConnectionString'
        secretref: azureServicesAuthConnectionStringSecretRef
      }
      {
        name: 'RootTenantBlobStorageConfigurationOptions__RootTenantBlobStorageConfiguration__KeyVaultName'
        value: keyVaultName
      }
      {
        name: 'RootTenantBlobStorageConfigurationOptions__RootTenantBlobStorageConfiguration__AccountKeySecretName'
        value: storageSecretName
      }
      {
        name: 'RootTenantBlobStorageConfigurationOptions__AccountName'
        value: storageName
      }
    ]
    // When using an ACR the key need not be passed to this module as a parameter
    // as we can look it up via the Azure resource
    containerRegistryKey: useAzureContainerRegistry ? acr.listCredentials().passwords[0].value : containerRegistryKey
    containerRegistryServer: useAzureContainerRegistry ? acr.properties.loginServer : containerRegistryServer
    containerRegistryUser: useAzureContainerRegistry ? acr.listCredentials().username : containerRegistryUser
    resourceTags: resourceTags
  }
}

output id string = tenancy_service.outputs.id
output name string = tenancy_service.outputs.name
output fqdn string = tenancy_service.outputs.fqdn
