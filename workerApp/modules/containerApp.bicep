param location string
param kubeEnvironmentId string
param containerImage string
param name string
param acrName string
param acrKey string
param ingressIsExternal bool
param ingressTargetPort int
param includeDapr bool
param daprSecrets object
param daprComponents object
param environmentVariables array = []

resource worker_app 'Microsoft.Web/containerApps@2021-03-01' = {
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
      // secrets: daprSecrets
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
          server: '${acrName}.azurecr.io'
          username: acrName
          password: acrKey
        }
      ]
      dapr: {
        enabled: includeDapr
        // appId: name
        // appPort: ingressTargetPort
        // components: daprComponents
      }
    }
  }
}
