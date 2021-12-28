param name string
param sku string = 'standard'

param access_policies array = []
param location string = resourceGroup().location

param resource_tags object = {}

resource key_vault 'Microsoft.KeyVault/vaults@2021-06-01-preview' = {
  name: name
  location: location
  properties: {
    sku: {
      family: 'A'
      name: sku
    }
    tenantId: tenant().tenantId
    accessPolicies: access_policies
  }
  tags: resource_tags
}
