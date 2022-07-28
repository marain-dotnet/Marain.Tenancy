// General Tenancy deployment parameters
param tenancyResourceGroupName string
param resourceTags object
param tenancystorageAccountName string
param tenancystorageSku string = 'Standard_LRS'
param tenancyAppName string
param tenancyStorageSecretName string
param tenancyFunctionStorageAccountName string = ''
param tenancyAppVersion string
param enableAuth bool = true

// Key Vault related parameters
param useExistingKeyVault bool = false
param tenancyKeyVaultName string
param tenancyKeyVaultResourceGroupName string = tenancyResourceGroupName
param keyVaultSecretsReadersGroupObjectId string
param keyVaultSecretsContributorsGroupObjectId string

// AppConfig-related parameters
param appConfigurationStoreName string
param appConfigurationStoreResourceGroupName string
param appConfigurationStoreSubscription string = subscription().subscriptionId
param appConfigurationLabel string

// Hosting-related parameters
param tenancyHostingPlatformResourceGroupName string = tenancyResourceGroupName
param tenancyHostingPlatformName string
@allowed([
  'Functions'
])
param hostingPlatformType string = 'Functions'
param tenancyUseExistingHosting bool = false

// Monitoring-related parameters
param appInsightsInstrumentationKeySecretName string = 'AppInsightsInstrumentationKey'

// AAD-related parameters
param aadDeploymentManagedIdentityName string
param aadDeploymentManagedIdentityResourceGroupName string
param aadDeploymentManagedIdentitySubscriptionId string = subscription().subscriptionId
param tenancyAdminSpName string
param tenancyAdminSpCredentialSecretName string
param tenancyAppRegistrationName string

// workaround to simplify parameter handling when we run the ARM deployment
param azureLocation string = deployment().location
param location string = azureLocation


targetScope = 'subscription'


var useFunctions = hostingPlatformType == 'Functions'


module appconfig_rg 'br:endjin.azurecr.io/bicep/general/resource-group:1.0.1' = {
  name: 'appConfigRg'
  scope: subscription(appConfigurationStoreSubscription)
  params: {
    location: location
    name: appConfigurationStoreResourceGroupName
    resourceTags: resourceTags
  }
}

module tenancy_rg 'br:endjin.azurecr.io/bicep/general/resource-group:1.0.1' = {
  name: 'tenancyRg'
  params: {
    location: location
    name: tenancyResourceGroupName
    resourceTags: resourceTags
  }
}

module keyvault_rg 'br:endjin.azurecr.io/bicep/general/resource-group:1.0.1' = {
  name: 'keyVaultRg'
  params: {
    location: location
    name: tenancyKeyVaultResourceGroupName
    resourceTags: resourceTags
  }
}

module app_config 'br:endjin.azurecr.io/bicep/general/app-configuration:0.1.1' = {
  scope: resourceGroup(appConfigurationStoreSubscription, appConfigurationStoreResourceGroupName)
  name: 'appConfigStoreDeploy'
  params: {
    name: appConfigurationStoreName
    useExisting: true
    sku: 'Standard'
    location: location
    resourceTags: resourceTags
  }
  dependsOn: [
    appconfig_rg
  ]
}

module key_vault 'br:endjin.azurecr.io/bicep/general/key-vault-with-secrets-access-via-groups:1.0.3' = {
  scope: resourceGroup(tenancyKeyVaultResourceGroupName)
  name: 'keyVaultWithGroupsAccess'
  params: {
    name: tenancyKeyVaultName
    enableDiagnostics: true
    secretsReadersGroupObjectId: keyVaultSecretsReadersGroupObjectId
    secretsContributorsGroupObjectId: keyVaultSecretsContributorsGroupObjectId
    enabledForTemplateDeployment: true
    tenantId: tenant().tenantId
    location: location
    resourceTags: resourceTags
  }
  dependsOn: [
    keyvault_rg
  ]
}

module tenancy_storage 'br:endjin.azurecr.io/bicep/general/storage-account:1.0.2' = {
  scope: resourceGroup(tenancyResourceGroupName)
  name: tenancystorageAccountName
  params: {
    name: tenancystorageAccountName
    sku: tenancystorageSku
    saveAccessKeyToKeyVault: true
    keyVaultResourceGroupName: tenancyKeyVaultResourceGroupName
    keyVaultName: key_vault.outputs.name
    keyVaultAccessKeySecretName: tenancyStorageSecretName
    resource_tags: resourceTags
  }
  dependsOn: [
    tenancy_rg
  ]
}

module tenant_admin_service_principal 'modules/aad_service_principal_script.bicep' = {
  scope: resourceGroup(tenancyResourceGroupName)
  name: 'adminSpDeploy'
  params: {
    displayName: tenancyAdminSpName
    keyVaultName: tenancyKeyVaultName
    keyVaultSecretName: tenancyAdminSpCredentialSecretName
    managedIdentityResourceId: aad_managed_id.id
    location: location
    resourceTags: resourceTags
  }
  dependsOn: [
    tenancy_rg
  ]
}

module tenancy_service_functions_app './modules/tenancy-functions.bicep' = if (useFunctions) {
  scope: resourceGroup(tenancyResourceGroupName)
  name: 'tenancyInFunctions' 
  params: {
    appConfigurationLabel: appConfigurationLabel
    appConfigurationStoreName: appConfigurationStoreName
    appConfigurationStoreResourceGroupName: appConfigurationStoreResourceGroupName
    hostingEnvironmentName: tenancyHostingPlatformName
    useExistingHostingEnvironment: tenancyUseExistingHosting
    hostingEnvironmentResourceGroupName: tenancyHostingPlatformResourceGroupName
    keyVaultName: tenancyKeyVaultName
    keyVaultResourceGroupName: useExistingKeyVault ? tenancyKeyVaultResourceGroupName : tenancy_rg.outputs.name
    tenancyAppName: tenancyAppName
    tenancyAppVersion: tenancyAppVersion
    functionStorageAccountName: tenancyFunctionStorageAccountName
    storageName: tenancy_storage.outputs.name
    storageSecretName: tenancyStorageSecretName
    appInsightsInstrumentationKeySecretName: appInsightsInstrumentationKeySecretName
    location: location
    resourceTags: resourceTags
  }
  dependsOn: [
    tenancy_rg
    keyvault_rg
  ]
}

resource aad_managed_id 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' existing = {
  name: aadDeploymentManagedIdentityName
  scope: resourceGroup(aadDeploymentManagedIdentitySubscriptionId, aadDeploymentManagedIdentityResourceGroupName)
}

var tenancyAppReplyUrl = 'https://${tenancy_service_functions_app.outputs.fqdn}/.auth/login/aad/callback'
module tenancy_aad_app './modules/aad_app_deployment_script.bicep' = {
  scope: resourceGroup(tenancyResourceGroupName)
  name: 'aadAppScriptDeploy'
  params: {
    displayName: tenancyAppRegistrationName
    replyUrl: tenancyAppReplyUrl
    managedIdentityResourceId: aad_managed_id.id
    location: location
    resourceTags: resourceTags
  }
  dependsOn: [
    tenancy_rg
  ]
}

// Enable EasyAuth on the Tenancy service
module tenancy_function_auth './modules/function-app-auth.bicep' = if (useFunctions) {
  scope: resourceGroup(tenancyHostingPlatformResourceGroupName)
  name: 'tenancyFunctionAppAuth'
  params: {
    authEnabled: enableAuth
    aadClientId: tenancy_aad_app.outputs.application_id
    functionName: tenancy_service_functions_app.outputs.name
  }
}

module tenancy_aad_app_id_config_key 'br:endjin.azurecr.io/bicep/general/set-app-configuration-keys:1.0.1' = {
  scope: resourceGroup(appConfigurationStoreSubscription, appConfigurationStoreResourceGroupName)
  name: 'tenncyAadAppIdConfigKeyDeploy'
  params: {
    appConfigStoreName: appConfigurationStoreName
    label: appConfigurationLabel
    entries: [
      {
        name: 'TenancyAadAppId'
        value: tenancy_aad_app.outputs.application_id
      }
    ]
  }
}

// module init_tenancy './modules/init_marain_tenancy_deployment_script.bicep' = {
//   scope: resourceGroup(resourceGroupName)
//   name: 'initTenancyDeploy'
//   params: {
//     managedIdentityResourceId: aad_managed_id.id
//     servicePrincipalCredential: existing_key_vault.getSecret(tenancyAdminSpCredentialSecretName)
//     tenencyServiceAppId: tenancy_aad_app.outputs.application_id
//     tenencyServiceUri: 'https://${useContainerApps ? tenancy_service_container_app.outputs.service_url : tenancy_service_functions_app.outputs.service_url}/'
//     location: location
//     resourceTags: resourceTags
//   }
//   dependsOn: [
//     tenancy_rg
//   ]
// }


output tenancyServiceFqdn string = tenancy_service_functions_app.outputs.fqdn
output tenancyServiceManagedIdentityAppId string = tenancy_service_functions_app.outputs.managedIdentityAppId
output tenancyAppRegistrationAppId string = tenancy_aad_app.outputs.application_id
output tenancyAppRegistrationObjectId string = tenancy_aad_app.outputs.object_id
output tenancyAdminServicePrincipalAppId string = tenant_admin_service_principal.outputs.app_id
output tenancyAdminServicePrincipalObjectId string = tenant_admin_service_principal.outputs.object_id
