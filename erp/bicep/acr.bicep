param name string
param location string
param sku string
param adminUserEnabled bool = false
param resourceTags object = {}

targetScope = 'resourceGroup'

resource acr 'Microsoft.ContainerRegistry/registries@2021-09-01' = {
  name: name
  location: location
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: adminUserEnabled
  }
  tags: resourceTags
}

output id string = acr.id
output name string = acr.name
