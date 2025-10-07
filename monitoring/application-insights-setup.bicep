@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the Application Insights resource')
param applicationInsightsName string

@description('Name of the Log Analytics workspace')
param logAnalyticsWorkspaceName string

@description('Environment name (dev, staging, prod)')
param environment string = 'dev'

@description('Retention period in days for Application Insights data')
param retentionInDays int = 90

@description('Daily data cap in GB for Application Insights')
param dailyDataCapGB int = 10

@description('Email addresses for alert notifications')
param alertEmailAddresses array = []

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: retentionInDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
  tags: {
    Environment: environment
    Service: 'Marain.Tenancy'
    Component: 'Monitoring'
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    RetentionInDays: retentionInDays
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
  tags: {
    Environment: environment
    Service: 'Marain.Tenancy'
    Component: 'Monitoring'
  }
}

// Daily Cap for Application Insights
resource applicationInsightsBilling 'Microsoft.Insights/components/CurrentBillingFeatures@2015-05-01' = {
  parent: applicationInsights
  name: 'CurrentBillingFeatures'
  properties: {
    CurrentBillingFeatures: [
      'Basic'
    ]
    DataVolumeCap: {
      Cap: dailyDataCapGB
      WarningThreshold: 80
      ResetTime: 0 // UTC hour (0-23) when the cap resets
    }
  }
}

// Action Group for Alerts
resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = if (length(alertEmailAddresses) > 0) {
  name: '${applicationInsightsName}-alerts'
  location: 'global'
  properties: {
    groupShortName: 'TenancyAlerts'
    enabled: true
    emailReceivers: [for email in alertEmailAddresses: {
      name: 'email-${uniqueString(email)}'
      emailAddress: email
      useCommonAlertSchema: true
    }]
  }
  tags: {
    Environment: environment
    Service: 'Marain.Tenancy'
    Component: 'Alerting'
  }
}

// High Error Rate Alert
resource highErrorRateAlert 'Microsoft.Insights/scheduledQueryRules@2021-08-01' = if (length(alertEmailAddresses) > 0) {
  name: '${applicationInsightsName}-high-error-rate'
  location: location
  properties: {
    displayName: 'Marain.Tenancy - High Error Rate'
    description: 'Alert when tenant operation error rate exceeds 5% over 5 minutes'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT10M'
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    scopes: [
      applicationInsights.id
    ]
    criteria: {
      allOf: [
        {
          query: '''
            let errorRate = customMetrics
            | where name contains "operations.total"
            | extend Status = tostring(customDimensions["status"])
            | summarize 
                ErrorOps = sumif(value, Status == "error"),
                TotalOps = sum(value)
            | extend ErrorRatePercent = (ErrorOps * 100.0) / TotalOps
            | project ErrorRatePercent;
            errorRate
            | where ErrorRatePercent > 5
          '''
          timeAggregation: 'Maximum'
          metricMeasureColumn: 'ErrorRatePercent'
          operator: 'GreaterThan'
          threshold: 5
          failingPeriods: {
            numberOfEvaluationPeriods: 2
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// High Latency Alert  
resource highLatencyAlert 'Microsoft.Insights/scheduledQueryRules@2021-08-01' = if (length(alertEmailAddresses) > 0) {
  name: '${applicationInsightsName}-high-latency'
  location: location
  properties: {
    displayName: 'Marain.Tenancy - High Latency (P95 > 2000ms)'
    description: 'Alert when P95 operation duration exceeds 2000ms over 5 minutes'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT10M'
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    scopes: [
      applicationInsights.id
    ]
    criteria: {
      allOf: [
        {
          query: '''
            customMetrics
            | where name contains "operation.duration"
            | summarize P95 = percentile(value, 95)
            | where P95 > 2000
            | project P95
          '''
          timeAggregation: 'Maximum'
          metricMeasureColumn: 'P95'
          operator: 'GreaterThan'
          threshold: 2000
          failingPeriods: {
            numberOfEvaluationPeriods: 2
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// Service Unavailable Alert
resource serviceUnavailableAlert 'Microsoft.Insights/scheduledQueryRules@2021-08-01' = if (length(alertEmailAddresses) > 0) {
  name: '${applicationInsightsName}-service-unavailable'
  location: location
  properties: {
    displayName: 'Marain.Tenancy - Service Unavailable'
    description: 'Alert when no successful operations detected for 10 minutes'
    severity: 1
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT10M'
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    scopes: [
      applicationInsights.id
    ]
    criteria: {
      allOf: [
        {
          query: '''
            let successfulOps = customMetrics
            | where name contains "operations.total"
            | where customDimensions["status"] == "success"
            | summarize SuccessCount = sum(value);
            successfulOps
            | extend ServiceAvailable = iff(SuccessCount > 0, 1, 0)
            | project ServiceAvailable
          '''
          timeAggregation: 'Maximum'
          metricMeasureColumn: 'ServiceAvailable'
          operator: 'LessThan'
          threshold: 1
          failingPeriods: {
            numberOfEvaluationPeriods: 2
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// Critical Errors Alert
resource criticalErrorsAlert 'Microsoft.Insights/scheduledQueryRules@2021-08-01' = if (length(alertEmailAddresses) > 0) {
  name: '${applicationInsightsName}-critical-errors'
  location: location
  properties: {
    displayName: 'Marain.Tenancy - Critical Errors'
    description: 'Alert on any critical level exceptions or errors'
    severity: 0 // Critical
    enabled: true
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    scopes: [
      applicationInsights.id
    ]
    criteria: {
      allOf: [
        {
          query: '''
            union exceptions, traces
            | where severityLevel >= 4
            | summarize CriticalErrorCount = count()
            | project CriticalErrorCount
          '''
          timeAggregation: 'Total'
          metricMeasureColumn: 'CriticalErrorCount'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// Storage Growth Alert
resource storageGrowthAlert 'Microsoft.Insights/scheduledQueryRules@2021-08-01' = if (length(alertEmailAddresses) > 0) {
  name: '${applicationInsightsName}-storage-growth'
  location: location
  properties: {
    displayName: 'Marain.Tenancy - Excessive Storage Growth'
    description: 'Alert when storage growth exceeds 10GB per day'
    severity: 3
    enabled: true
    evaluationFrequency: 'PT1H'
    windowSize: 'PT24H'
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    scopes: [
      applicationInsights.id
    ]
    criteria: {
      allOf: [
        {
          query: '''
            customMetrics
            | where name == "tenant.storage.blob.size"
            | summarize TotalStorageBytes = sum(value) by bin(timestamp, 1d)
            | sort by timestamp asc
            | extend StorageGB = TotalStorageBytes / (1024*1024*1024)
            | extend GrowthRateGB = StorageGB - prev(StorageGB)
            | where isnotempty(GrowthRateGB) and GrowthRateGB > 10
            | project GrowthRateGB
          '''
          timeAggregation: 'Maximum'
          metricMeasureColumn: 'GrowthRateGB'
          operator: 'GreaterThan'
          threshold: 10
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// Outputs
output applicationInsightsId string = applicationInsights.id
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
output applicationInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output actionGroupId string = length(alertEmailAddresses) > 0 ? actionGroup.id : ''

// Dashboard deployment (optional)
output dashboardDeploymentCommand string = 'az portal dashboard import --input-path monitoring/application-insights-dashboard.json --resource-group ${resourceGroup().name}'