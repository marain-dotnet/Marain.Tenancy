@description('The name of the app configuration store')
param name string
@description('The location of the app configuration store')
param location string
@description('When false, the app configuration store will be inaccessible via its public IP address')
param enablePublicNetworkAccess bool = true

@description('SKU for the app configuration store')
@allowed([
  'Free'
  'Standard'
])
param sku string = 'Standard'
param tagValues object = {}

var publicNetworkAccess = enablePublicNetworkAccess ? 'Enabled' : 'Disabled'

targetScope = 'resourceGroup'

resource app_config_store 'Microsoft.AppConfiguration/configurationStores@2020-06-01' = {
  name: name
  location: location
  sku: {
    name: sku
  }
  properties: {
    publicNetworkAccess: publicNetworkAccess
  }
  tags: tagValues
}

output id string = app_config_store.id
output name string = app_config_store.name
