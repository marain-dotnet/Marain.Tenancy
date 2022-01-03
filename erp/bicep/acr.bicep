param name string
param location string
param sku string
param adminUserEnabled bool = false
param saveAccessKeyToKeyVault bool = false
param keyVaultResourceGroupName string = ''
param keyVaultName string = ''
param keyVaultSecretName string = ''
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

module acr_key_secret 'key_vault_secret.bicep' = if (saveAccessKeyToKeyVault) {
  name: 'acrKeySecretDeploy'
  scope: resourceGroup(keyVaultResourceGroupName) 
  params: {
    keyVaultName: keyVaultName
    secretName: keyVaultSecretName
    contentValue: acr.listKeys().keys[0].value
  }
}

output id string = acr.id
output name string = acr.name
