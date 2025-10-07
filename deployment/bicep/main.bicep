targetScope = 'resourceGroup'

param acrName string
param acrResourceGroupName string
param acrSubscriptionId string = subscription().subscriptionId

param hostingEnvironmentName string
param hostingEnvironmentResourceGroupName string = resourceGroup().name

param appInsightsWorkspaceName string
param logAnalyticsWorkspaceName string

param tenancyKeyVaultName string
param tenancyServiceName string
param tenancyServiceContainerImageName string
param tenancyServiceContainerImageTag string = 'latest'
param tenancyServiceManagedIdentityName string
param tenancyServiceAppRegistrationName string = tenancyServiceName
param tenancyStorageAccountName string

param location string = resourceGroup().location
param resourceTags object = {}
param enableAvmTelemetry bool


// Variables
var metricsOnlyDiagsConfig = [
  {
    metricCategories: [
      {
        category: 'AllMetrics'
      }
    ]
    workspaceResourceId: log_analytics.outputs.resourceId
  }
]
var fullDiagsConfig = [
  {
    logCategoriesAndGroups: [
      {
        categoryGroup: 'AllLogs'
      }
    ]
    metricCategories: [
      {
        category: 'AllMetrics'
      }
    ]
    workspaceResourceId: log_analytics.outputs.resourceId
  }
]



// Existing resources
var _acrName = split(acrName, '.')[0]   // convenience feature to handle when the ACR name is supplied as an FQDN
resource acr 'Microsoft.ContainerRegistry/registries@2025-04-01' existing = {
  name: _acrName
  scope: resourceGroup(acrSubscriptionId, acrResourceGroupName)
}

// Potentially shared resources
module hosting_environment 'br/public:avm/res/app/managed-environment:0.11.3' = {
  scope: resourceGroup(hostingEnvironmentResourceGroupName)
  params: {
    appInsightsConnectionString: app_insights.outputs.connectionString
    enableTelemetry: enableAvmTelemetry
    internal: false
    location: location
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: log_analytics.outputs.logAnalyticsWorkspaceId
        sharedKey: log_analytics_resource.listKeys().primarySharedKey
      }
    }
    managedIdentities: {
      userAssignedResourceIds: [
        managed_identity.outputs.resourceId
      ]
    }
    name: hostingEnvironmentName
    openTelemetryConfiguration: {
      tracesConfiguration: {
        destinations: [
          // 'appInsights'
        ]
      }
      logsConfiguration: {
        destinations: [
          // 'appInsights'
        ]
      }
    }
    publicNetworkAccess: 'Enabled'
    tags: resourceTags
    workloadProfiles: [
      {
        name: 'Consumption'
        workloadProfileType: 'Consumption'
      }
    ]
    zoneRedundant: false
  }
}

module log_analytics 'br/public:avm/res/operational-insights/workspace:0.9.1' = {
  name: 'deployLogAnalytics'
  params: {
    dailyQuotaGb: 5
    dataRetention: 30
    diagnosticSettings: []
    enableTelemetry: enableAvmTelemetry
    location: location
    name: logAnalyticsWorkspaceName
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    roleAssignments: []
    tags: resourceTags
    useResourcePermissions: false
  }
}
// We need access to the actual Log Analytics workspace resource in order to queries it's keys etc.,
// so whenever referencing this resource we must ensure that the referencing resource/module has a
// dependency on the above module.
resource log_analytics_resource 'Microsoft.OperationalInsights/workspaces@2025-02-01' existing = {
  name: logAnalyticsWorkspaceName
}

module app_insights 'br/public:avm/res/insights/component:0.4.2' = {
  name: 'deployAppInsights'
  params: {
    diagnosticSettings: []
    enableTelemetry: enableAvmTelemetry
    location: location
    name: appInsightsWorkspaceName
    roleAssignments: []
    tags: resourceTags
    workspaceResourceId: log_analytics.outputs.resourceId
  }
}



// Tenancy service resources
module managed_identity 'br/public:avm/res/managed-identity/user-assigned-identity:0.4.1' = {
  params: {
    enableTelemetry: enableAvmTelemetry
    location: location
    name: tenancyServiceManagedIdentityName
    roleAssignments: []
    tags: resourceTags
  }
}

// When we first create the app registration we don't know the values for the following settings:
// - IdentifierUris: Given the restrictions on the values for this, we need to use the AppId which we can't know until it's created
//
// NOTE: On subsequent deployments (i.e. the app registration is already fully setup), then this module will briefly reset the above properties, until the
//       the stage2 runs. TODO: Investigate whether the 'onlyIfNotExists' experimental feature could be used to mitigate this.
module tenancy_app_registration_stage1 'app-registration.bicep' = {
  params: {
    appRegistrationName: tenancyServiceAppRegistrationName
  }
}

// This will update the app registration created above and its dependencies will ensure it runs at the correct time.
module tenancy_app_registration_stage2 'app-registration.bicep' = {
  params: {
    appRegistrationName: tenancyServiceAppRegistrationName
    identifierUris: [
      'api://${tenancy_app_registration_stage1.outputs.clientId}'
    ]
  }
}

module tenancy_storage 'br/public:avm/res/storage/storage-account:0.26.2' = {
  params: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    blobServices: {
      containers: []
      diagnosticSettings: fullDiagsConfig
    }
    diagnosticSettings: metricsOnlyDiagsConfig
    enableTelemetry: enableAvmTelemetry
    kind: 'StorageV2'
    location: location
    minimumTlsVersion: 'TLS1_2'
    name: tenancyStorageAccountName
    networkAcls: {
      resourceAccessRules: []
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    publicNetworkAccess: 'Enabled'
    roleAssignments: [
      {
        principalId: managed_identity.outputs.principalId
        roleDefinitionIdOrName: 'Storage Blob Data Contributor'
      }
    ]
    skuName: 'Standard_LRS'
    supportsHttpsTrafficOnly: true
    tags: resourceTags
  }
}

module key_vault 'br/public:avm/res/key-vault/vault:0.13.3' = {
  name: 'deployKeyVault'
  params: {
    name: tenancyKeyVaultName
    accessPolicies: []
    diagnosticSettings: fullDiagsConfig
    enablePurgeProtection: false
    enableRbacAuthorization: true
    enableSoftDelete: true
    enableTelemetry: enableAvmTelemetry
    location: location
    networkAcls: {}
    privateEndpoints: []
    roleAssignments: []
    secrets: []
    softDeleteRetentionInDays: 7
    tags: resourceTags
  }
}

var tenancyServiceInternalPort = 8080
var _tenancyServiceUrl = 'https://${tenancy_service_app.outputs.fqdn}'
module tenancy_service_app 'br/public:avm/res/app/container-app:0.18.1' = {
  params: {
    activeRevisionsMode: 'Single'
    containers: [
      {
        env: [
          {
            name: 'AzureAd__AllowWebApiToBeAuthorizedByACL'
            value: 'true'
          }
          {
            name: 'AzureAd__Audience'
            value: tenancy_app_registration_stage1.outputs.clientId
          }
          {
            name: 'AzureAd__ClientId'
            value: tenancy_app_registration_stage1.outputs.clientId
          }
          {
            name: 'AzureAd__Instance'
            value: environment().authentication.loginEndpoint
          }
          {
            name: 'AzureAd__TenantId'
            value: tenant().tenantId
          }
          {
            name: 'ASPNETCORE_URLS'
            value: 'http://*:${tenancyServiceInternalPort}'
          }
          {
            name: 'ConnectionStrings__ApplicationInsights'
            value: app_insights.outputs.connectionString
          }
          {
            name: 'Logging__LogLevel__Default'
            value: 'Information'
          }
          {
            name: 'OpenTelemetry__ResourceAttributes__service.name'
            value: 'Marain.Tenancy.Api'
          }
          {
            name: 'OpenTelemetry__ResourceAttributes__service.version'
            value: '1.0'
          }
          {
            name: 'RootBlobStorageConfiguration__AccountName'
            value: tenancy_storage.outputs.name
          }
          {
            name: 'RootBlobStorageConfiguration__ClientIdentity__IdentitySourceType'
            value: 'UserAssignedManaged'
          }
          {
            name: 'RootBlobStorageConfiguration__ClientIdentity__ManagedIdentityClientId'
            value: managed_identity.outputs.clientId
          }
          {
            name: 'TenantCacheConfiguration__GetTenantResponseCacheDurationSeconds'
            value: '300'
          }

        ]
        image: '${acr.properties.loginServer}/${tenancyServiceContainerImageName}:${tenancyServiceContainerImageTag}'
        name: 'tenancy-service'
        probes: [
          {
            httpGet: {
              path: '/health'
              port: tenancyServiceInternalPort
              scheme: 'HTTP'
            }
          }
        ]
        resources: {
          cpu: json('0.5')
          memory: '1Gi'
        }
      }
    ]
    diagnosticSettings: metricsOnlyDiagsConfig
    disableIngress: false
    enableTelemetry: enableAvmTelemetry
    environmentResourceId: hosting_environment.outputs.resourceId
    ingressAllowInsecure: false
    ingressExternal: true
    ingressTargetPort: tenancyServiceInternalPort
    location: location
    managedIdentities: {
      userAssignedResourceIds: [
        managed_identity.outputs.resourceId
      ]
    }
    name: tenancyServiceName
    registries: [
      {
        identity: managed_identity.outputs.resourceId
        server: acr.properties.loginServer
      }
    ]
    roleAssignments: []
    runtime: {}
    scaleSettings: {
      maxReplicas: 1
      minReplicas: 0
    }
    secrets: []
    tags: resourceTags
  }
}

// TODO:
// - Ensure UAMI has permissions to ACR (e.g. added to Entra group)

output tenancyServiceManagedIdentityPrincipalId string = managed_identity.outputs.principalId
output tenancyServiceUrl string = _tenancyServiceUrl
