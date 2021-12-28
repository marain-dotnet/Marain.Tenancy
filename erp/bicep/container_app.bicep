param location string
param kubeEnvironmentId string
param containerImage string
param name string
param crServer string
param crUser string
param crKey string
param ingressIsExternal bool
param ingressTargetPort int
param includeDapr bool
param daprSecrets object
param daprComponents object
param environmentVariables array = []

resource worker_app 'Microsoft.Web/containerapps@2021-03-01' = {
  name: name
  kind: 'workerapp'
  location: location
  properties: {
    kubeEnvironmentId: kubeEnvironmentId
    configuration: {
      ingress: {
        external: ingressIsExternal 
        targetPort: ingressTargetPort
      }
      secrets: daprSecrets
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
        minReplicas: 1
        maxReplicas: 1
      }
      registries: [
        {
          server: crServer
          username: crUser
          password: crKey
        }
      ]
      // Selectively include dapr-related properties
      dapr: includeDapr ? {
        enabled: true
        appId: name
        appPort: ingressTargetPort
        components: daprComponents
      } : {
        enabled: false
      }
    }
  }
}
