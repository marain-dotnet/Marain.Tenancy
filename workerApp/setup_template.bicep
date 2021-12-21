param location string
param workspace_name string
param appinsights_name string

resource workspace_name_resource 'Microsoft.OperationalInsights/workspaces@2020-08-01' = {
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
}

resource appinsights_name_resource 'microsoft.insights/components@2020-02-02-preview' = {
  name: appinsights_name
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Redfield'
    Request_Source: 'CustomDeployment'
  }
}
