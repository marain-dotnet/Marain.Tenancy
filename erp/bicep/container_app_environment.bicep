param location string
param workspace_name string
param appinsights_name string
param environment_name string

param include_container_registry bool = false
param container_registry_name string = '${environment_name}acr'
param enable_container_registry_adminuser bool = false

param resource_tags object = {}

resource acr 'Microsoft.ContainerRegistry/registries@2021-06-01-preview' = if (include_container_registry) {
  name: container_registry_name
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    adminUserEnabled: enable_container_registry_adminuser
  }
  tags: resource_tags
}

resource log_analytics 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: workspace_name
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
  tags: resource_tags
}

resource app_insights 'Microsoft.Insights/components@2020-02-02' = {
  name: appinsights_name
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Redfield'
    Request_Source: 'CustomDeployment'
  }
  tags: resource_tags
}

resource container_app_environment 'Microsoft.Web/kubeEnvironments@2021-02-01' = {
  name: environment_name
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
  tags: resource_tags
}
