param location string
param kubeEnvironmentId string
param containerImage string
param name string
@allowed([
  'single'
  'multiple'
])
param activeRevisionsMode string = 'multiple'
param ingressIsExternal bool
param ingressTargetPort int
param includeDapr bool
param daprAppId string = name
param secrets array = []
param daprComponents array = []
param environmentVariables array = []
param minReplicas int = 0
param maxReplicas int = 1

param containerRegistryServer string = 'docker.io'
param containerRegistryUser string
@secure()
param containerRegistryKey string
param resourceTags object = {}

targetScope = 'resourceGroup'

var imagePullSecretRef = 'container-registry-pull-secret'
var imagePullSecret = {
  name: imagePullSecretRef
  value: containerRegistryKey
}
var containerAppSecrets = concat(array(imagePullSecret), secrets)

resource container_app 'Microsoft.Web/containerapps@2021-03-01' = {
  name: name
  kind: 'containerapps'
  location: location
  properties: {
    kubeEnvironmentId: kubeEnvironmentId
    configuration: {
      activeRevisionsMode: activeRevisionsMode
      ingress: {
        external: ingressIsExternal 
        targetPort: ingressTargetPort
      }
      secrets: containerAppSecrets
      registries: [
        {
          passwordSecretRef: imagePullSecretRef
          server: containerRegistryServer
          username: containerRegistryUser
        }
      ]
    }
    template: {
      containers: [
        {
          image: containerImage
          name: name
          env: environmentVariables
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
      // Selectively include dapr-related properties
      dapr: includeDapr ? {
        enabled: true
        appId: daprAppId
        appPort: ingressTargetPort
        components: daprComponents
      } : {
        enabled: false
      }
    }
  }
  tags: resourceTags
}

output id string = container_app.id
output name string = container_app.name
output fqdn string = container_app.properties.configuration.ingress.fqdn
