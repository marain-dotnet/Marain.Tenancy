param location string
param workspaceName string
param appinsightsName string
param environmentName string

param createContainerRegistry bool = false
param containerRegistryName string = '${environmentName}acr'
param containerRegistrySku string = 'Standard'
param enableContainerRegistryAdminUser bool = true

param resourceTags object = {}

targetScope = 'resourceGroup'

module acr 'acr.bicep' = if (createContainerRegistry) {
  name: 'appEnvAcrDeploy'
  params: {
    name: containerRegistryName
    location: location
    sku: containerRegistrySku
    adminUserEnabled: enableContainerRegistryAdminUser
    resourceTags: resourceTags
  }
}

resource log_analytics 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      searchVersion: 1
      legacy: 0
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
  tags: resourceTags
}

resource app_insights 'Microsoft.Insights/components@2020-02-02' = {
  name: appinsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Redfield'
    Request_Source: 'CustomDeployment'
  }
  tags: resourceTags
}

resource container_app_environment 'Microsoft.Web/kubeEnvironments@2021-02-01' = {
  name: environmentName
  location: location
  properties: {
    type: 'managed'
    internalLoadBalancerEnabled: false
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: log_analytics.properties.customerId
        sharedKey: listKeys(log_analytics.id, log_analytics.apiVersion).primarySharedKey
      }
    }
    containerAppsConfiguration: {
      daprAIInstrumentationKey: app_insights.properties.InstrumentationKey
    }
  }
  tags: resourceTags
}

output id string = container_app_environment.id
output name string = container_app_environment.name
output appinsights_instrumentation_key string = app_insights.properties.InstrumentationKey
