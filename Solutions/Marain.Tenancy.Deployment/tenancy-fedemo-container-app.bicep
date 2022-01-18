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
@secure()
param servicePrincipalCredential string
@secure()
param appInsightsInstrumentationKey string
param activeRevisionsMode string = 'single'
param minReplicas int = 0
param maxReplicas int = 1


targetScope = 'resourceGroup'


var containerImage = '${imageName}:${imageTag}'
// parse the keyvault secret into the its constituent parts
var spClientId = json(servicePrincipalCredential).appId
var spClientSecret = json(servicePrincipalCredential).password
var spTenantId = json(servicePrincipalCredential).tenant
var servicePrincipalPasswordSecretRef = 'service-principal-secret'

resource acr 'Microsoft.ContainerRegistry/registries@2021-09-01' existing = if (useAzureContainerRegistry) {
  name: acrName
  scope: resourceGroup(acrSubscription, acrResourceGroupName)
}

module tenancy_fedemo '../../erp/bicep/container_app.bicep' = {
  name: 'tenancyFeDemoContainerAppDeploy'
  params: {
    location: location
    containerImage: useAzureContainerRegistry ? '${acr.properties.loginServer}/${containerImage}' : '${containerRegistryServer}/${containerImage}'
    activeRevisionsMode: activeRevisionsMode
    daprComponents: []
    secrets: [
      {
        name: servicePrincipalPasswordSecretRef
        value: spClientSecret
      }
    ]
    ingressIsExternal: true
    ingressTargetPort: 80
    kubeEnvironmentId: kubeEnvironmentId
    name: appName
    includeDapr: true
    daprAppId: 'demofe'
    environmentVariables: [
      {
        name: 'ServiceIdentity__IdentitySourceType'
        value: 'AzureIdentityDefaultAzureCredential'
      }
      {
        name: 'AZURE_CLIENT_SECRET'
        secretref: servicePrincipalPasswordSecretRef
      }
      {
        name: 'AZURE_CLIENT_ID'
        value: spClientId
      }
      {
        name: 'AZURE_TENANT_ID'
        value: spTenantId
      }
      {
        name: 'ApplicationInsights__InstrumentationKey'
        value: appInsightsInstrumentationKey
      }
    ]
    // When using an ACR the key need not be passed to this module as a parameter
    // as we can look it up via the Azure resource
    containerRegistryKey: useAzureContainerRegistry ? acr.listCredentials().passwords[0].value : containerRegistryKey
    containerRegistryServer: useAzureContainerRegistry ? acr.properties.loginServer : containerRegistryServer
    containerRegistryUser: useAzureContainerRegistry ? acr.listCredentials().username : containerRegistryUser
    minReplicas: minReplicas
    maxReplicas: maxReplicas
    resourceTags: resourceTags
  }
}

output id string = tenancy_fedemo.outputs.id
output name string = tenancy_fedemo.outputs.name
output fqdn string = tenancy_fedemo.outputs.fqdn
