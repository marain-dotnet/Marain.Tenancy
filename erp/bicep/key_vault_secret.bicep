@description('Enter the secret name.')
param secretName string

@description('Type of the secret')
param contentType string = 'text/plain'

@description('Value of the secret')
param contentValue string

@description('Name of the vault')
param keyVaultName string

targetScope = 'resourceGroup'

resource key_vault 'Microsoft.KeyVault/vaults@2021-06-01-preview' existing = {
  name: keyVaultName
}

resource key_vault_secret 'Microsoft.KeyVault/vaults/secrets@2021-06-01-preview' = {
  name: secretName
  parent: key_vault
  properties: {
    contentType: contentType
    value: contentValue
  }
}
