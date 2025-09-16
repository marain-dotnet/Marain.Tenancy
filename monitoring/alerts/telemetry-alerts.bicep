// Bicep template for Application Insights alerts for Marain.Tenancy telemetry monitoring

@description('The name of the Application Insights resource')
param applicationInsightsName string

@description('The resource group of the Application Insights resource')
param applicationInsightsResourceGroup string = resourceGroup().name

@description('Email addresses for alert notifications')
param alertEmailAddresses array = []

@description('Teams webhook URL for alert notifications')
param teamsWebhookUrl string = ''

@description('Environment name (dev, test, prod)')
param environment string = 'prod'

// Reference to existing Application Insights resource
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
  scope: resourceGroup(applicationInsightsResourceGroup)
}

// Action Group for notifications
resource alertActionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'ag-marain-tenancy-${environment}'
  location: 'Global'
  properties: {
    groupShortName: 'MarainTenancy'
    enabled: true
    emailReceivers: [for email in alertEmailAddresses: {
      name: replace(email, '@', '-at-')
      emailAddress: email
      useCommonAlertSchema: true
    }]
    webhookReceivers: teamsWebhookUrl != '' ? [{
      name: 'TeamsWebhook'
      serviceUri: teamsWebhookUrl
      useCommonAlertSchema: true
    }] : []
  }
}

// High Error Rate Alert
resource highErrorRateAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'alert-tenant-high-error-rate-${environment}'
  location: resourceGroup().location
  properties: {
    displayName: 'Marain.Tenancy - High Error Rate'
    description: 'Alert when tenant operations have error rate > 5% over 5 minutes'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    scopes: [
      applicationInsights.id
    ]
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    criteria: {
      allOf: [
        {
          query: '''
            requests
            | where timestamp >= ago(5m)
            | where name contains "tenant"
            | where customDimensions["service.name"] == "Marain.Tenancy"
            | summarize 
                TotalRequests = count(),
                FailedRequests = countif(success == false)
            | extend ErrorRate = round(100.0 * FailedRequests / TotalRequests, 2)
            | where ErrorRate > 5
          '''
          timeAggregation: 'Total'
          metricMeasureColumn: 'ErrorRate'
          threshold: 5
          operator: 'GreaterThan'
          resourceIdColumn: '_ResourceId'
          failingPeriods: {
            numberOfEvaluationPeriods: 2
            minFailingPeriodsToAlert: 2
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        alertActionGroup.id
      ]
    }
  }
}

// Slow Response Time Alert
resource slowResponseAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'alert-tenant-slow-response-${environment}'
  location: resourceGroup().location
  properties: {
    displayName: 'Marain.Tenancy - Slow Response Times'
    description: 'Alert when 95th percentile response time > 5000ms for tenant operations'
    severity: 3
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT10M'
    scopes: [
      applicationInsights.id
    ]
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    criteria: {
      allOf: [
        {
          query: '''
            requests
            | where timestamp >= ago(10m)
            | where name contains "tenant"
            | where customDimensions["service.name"] == "Marain.Tenancy"
            | summarize P95Duration = percentile(duration, 95)
            | where P95Duration > 5000
          '''
          timeAggregation: 'Average'
          metricMeasureColumn: 'P95Duration'
          threshold: 5000
          operator: 'GreaterThan'
          resourceIdColumn: '_ResourceId'
          failingPeriods: {
            numberOfEvaluationPeriods: 2
            minFailingPeriodsToAlert: 2
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        alertActionGroup.id
      ]
    }
  }
}

// Low Success Rate Alert
resource lowSuccessRateAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'alert-tenant-low-success-rate-${environment}'
  location: resourceGroup().location
  properties: {
    displayName: 'Marain.Tenancy - Low Success Rate'
    description: 'Alert when tenant operations success rate < 95% over 10 minutes'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT2M'
    windowSize: 'PT10M'
    scopes: [
      applicationInsights.id
    ]
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    criteria: {
      allOf: [
        {
          query: '''
            traces
            | where timestamp >= ago(10m)
            | where customDimensions["service.name"] == "Marain.Tenancy"
            | where customDimensions["operation.type"] in ("create", "get", "delete")
            | extend Success = customDimensions["activity.status"] == "Ok"
            | summarize 
                TotalOps = count(),
                SuccessfulOps = countif(Success)
            | extend SuccessRate = round(100.0 * SuccessfulOps / TotalOps, 2)
            | where SuccessRate < 95
          '''
          timeAggregation: 'Average'
          metricMeasureColumn: 'SuccessRate'
          threshold: 95
          operator: 'LessThan'
          resourceIdColumn: '_ResourceId'
          failingPeriods: {
            numberOfEvaluationPeriods: 2
            minFailingPeriodsToAlert: 2
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        alertActionGroup.id
      ]
    }
  }
}

// Telemetry Export Failure Alert
resource telemetryExportFailureAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'alert-telemetry-export-failure-${environment}'
  location: resourceGroup().location
  properties: {
    displayName: 'Marain.Tenancy - Telemetry Export Failures'
    description: 'Alert when telemetry export failure rate is high'
    severity: 3
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    scopes: [
      applicationInsights.id
    ]
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    criteria: {
      allOf: [
        {
          query: '''
            traces
            | where timestamp >= ago(15m)
            | where message contains "export" or message contains "telemetry"
            | where severityLevel >= 3  // Warning or Error
            | where customDimensions["service.name"] == "Marain.Tenancy"
            | summarize ExportIssues = count()
            | where ExportIssues > 10
          '''
          timeAggregation: 'Total'
          metricMeasureColumn: 'ExportIssues'
          threshold: 10
          operator: 'GreaterThan'
          resourceIdColumn: '_ResourceId'
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        alertActionGroup.id
      ]
    }
  }
}

// High Memory Usage Alert
resource highMemoryUsageAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'alert-high-memory-usage-${environment}'
  location: resourceGroup().location
  properties: {
    displayName: 'Marain.Tenancy - High Memory Usage'
    description: 'Alert when application memory usage is consistently high'
    severity: 3
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    scopes: [
      applicationInsights.id
    ]
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    criteria: {
      allOf: [
        {
          query: '''
            performanceCounters
            | where timestamp >= ago(15m)
            | where name == "Private Bytes" or name == "Process Private Bytes"
            | where customDimensions["service.name"] == "Marain.Tenancy"
            | summarize AvgMemoryMB = avg(value) / (1024 * 1024)
            | where AvgMemoryMB > 1000  // Alert if > 1GB average
          '''
          timeAggregation: 'Average'
          metricMeasureColumn: 'AvgMemoryMB'
          threshold: 1000
          operator: 'GreaterThan'
          resourceIdColumn: '_ResourceId'
          failingPeriods: {
            numberOfEvaluationPeriods: 3
            minFailingPeriodsToAlert: 3
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        alertActionGroup.id
      ]
    }
  }
}

// Dependency Failure Alert
resource dependencyFailureAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'alert-dependency-failures-${environment}'
  location: resourceGroup().location
  properties: {
    displayName: 'Marain.Tenancy - Dependency Failures'
    description: 'Alert when external dependency calls are failing frequently'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT2M'
    windowSize: 'PT10M'
    scopes: [
      applicationInsights.id
    ]
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    criteria: {
      allOf: [
        {
          query: '''
            dependencies
            | where timestamp >= ago(10m)
            | where customDimensions["service.name"] == "Marain.Tenancy"
            | where type in ("Azure blob", "HTTP", "SQL")
            | summarize 
                TotalCalls = count(),
                FailedCalls = countif(success == false)
            | extend FailureRate = round(100.0 * FailedCalls / TotalCalls, 2)
            | where FailureRate > 10
          '''
          timeAggregation: 'Average'
          metricMeasureColumn: 'FailureRate'
          threshold: 10
          operator: 'GreaterThan'
          resourceIdColumn: '_ResourceId'
          failingPeriods: {
            numberOfEvaluationPeriods: 2
            minFailingPeriodsToAlert: 2
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        alertActionGroup.id
      ]
    }
  }
}

// Availability Alert
resource availabilityAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'alert-low-availability-${environment}'
  location: resourceGroup().location
  properties: {
    displayName: 'Marain.Tenancy - Low Availability'
    description: 'Alert when service availability drops below 99%'
    severity: 1
    enabled: true
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    scopes: [
      applicationInsights.id
    ]
    targetResourceTypes: [
      'Microsoft.Insights/components'
    ]
    criteria: {
      allOf: [
        {
          query: '''
            availabilityResults
            | where timestamp >= ago(5m)
            | where customDimensions["service.name"] == "Marain.Tenancy"
            | summarize AvailabilityPercentage = avg(toint(success)) * 100
            | where AvailabilityPercentage < 99
          '''
          timeAggregation: 'Average'
          metricMeasureColumn: 'AvailabilityPercentage'
          threshold: 99
          operator: 'LessThan'
          resourceIdColumn: '_ResourceId'
          failingPeriods: {
            numberOfEvaluationPeriods: 2
            minFailingPeriodsToAlert: 2
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        alertActionGroup.id
      ]
    }
  }
}

// Output alert rule resource IDs
output alertActionGroupId string = alertActionGroup.id
output highErrorRateAlertId string = highErrorRateAlert.id
output slowResponseAlertId string = slowResponseAlert.id
output lowSuccessRateAlertId string = lowSuccessRateAlert.id
output telemetryExportFailureAlertId string = telemetryExportFailureAlert.id
output highMemoryUsageAlertId string = highMemoryUsageAlert.id
output dependencyFailureAlertId string = dependencyFailureAlert.id
output availabilityAlertId string = availabilityAlert.id