param name string
param location string = resourceGroup().location
param sku string = 'Standard_LRS'
param kind string = 'StorageV2'
param tlsVersion string = 'TLS1_2'
param httpsOnly bool = true
param accessTier string = 'Hot'
param resource_tags object = {}


resource storage_account 'Microsoft.Storage/storageAccounts@2021-06-01' = {
  name: name
  location: location
  sku: {
    name: sku
  }
  kind: kind
  properties: {
    minimumTlsVersion: tlsVersion
    supportsHttpsTrafficOnly: httpsOnly
    accessTier: accessTier
  }
  tags: resource_tags
}
