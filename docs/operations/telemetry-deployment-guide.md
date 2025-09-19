# Telemetry Deployment & Operations Guide

This guide provides step-by-step instructions for deploying and operating the OpenTelemetry implementation in Marain.Tenancy.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Deployment Steps](#deployment-steps)
- [Configuration Management](#configuration-management)
- [Monitoring Setup](#monitoring-setup)
- [Operations Procedures](#operations-procedures)
- [Troubleshooting](#troubleshooting)
- [Maintenance](#maintenance)

## Prerequisites

### Azure Resources Required

1. **Application Insights Instance**
   - Standard pricing tier recommended for production
   - Same region as application deployment
   - Data retention configured appropriately

2. **Log Analytics Workspace** 
   - Linked to Application Insights
   - Appropriate retention and daily cap settings

3. **Service Principal or Managed Identity**
   - For secure access to Application Insights
   - Required permissions for telemetry ingestion

### Development Environment

- .NET 8.0 SDK
- Azure CLI or PowerShell with Azure modules
- Access to target Azure subscription
- Application deployment permissions

## Deployment Steps

### 1. Application Insights Setup

```bash
# Create Application Insights instance
az monitor app-insights component create \
  --app "marain-tenancy-insights" \
  --location "East US" \
  --resource-group "marain-tenancy-rg" \
  --application-type "web" \
  --retention-time 90

# Get connection string
APPINSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show \
  --app "marain-tenancy-insights" \
  --resource-group "marain-tenancy-rg" \
  --query connectionString -o tsv)
```

### 2. Configure Application Settings

#### For Azure App Service:
```bash
az webapp config appsettings set \
  --resource-group "marain-tenancy-rg" \
  --name "marain-tenancy-api" \
  --settings "ConnectionStrings__ApplicationInsights=$APPINSIGHTS_CONNECTION_STRING"
```

#### For Azure Functions:
```bash
az functionapp config appsettings set \
  --resource-group "marain-tenancy-rg" \
  --name "marain-tenancy-functions" \
  --settings "ConnectionStrings__ApplicationInsights=$APPINSIGHTS_CONNECTION_STRING"
```

#### For Container Apps:
```bash
az containerapp update \
  --name "marain-tenancy-api" \
  --resource-group "marain-tenancy-rg" \
  --set-env-vars "ConnectionStrings__ApplicationInsights=$APPINSIGHTS_CONNECTION_STRING"
```

### 3. Deploy Monitoring Alerts

```bash
# Deploy alert rules using Bicep template
az deployment group create \
  --resource-group "marain-tenancy-rg" \
  --template-file "./monitoring/alerts/telemetry-alerts.bicep" \
  --parameters applicationInsightsName="marain-tenancy-insights" \
               environment="prod" \
               alertEmailAddresses='["ops@company.com"]'
```

### 4. Deploy Application with Telemetry

```bash
# Build and deploy application
dotnet publish Solutions/Marain.Tenancy.Api/Marain.Tenancy.Api.csproj -c Release
az webapp deploy --resource-group "marain-tenancy-rg" \
                 --name "marain-tenancy-api" \
                 --src-path "./Solutions/Marain.Tenancy.Api/bin/Release/net8.0/publish"
```

### 5. Verify Telemetry Flow

```bash
# Test API endpoint to generate telemetry
curl -X GET "https://marain-tenancy-api.azurewebsites.net/health"

# Check Application Insights for data (wait 2-3 minutes)
az monitor app-insights query \
  --app "marain-tenancy-insights" \
  --analytics-query "requests | where timestamp >= ago(5m) | limit 10"
```

## Configuration Management

### Environment-Specific Settings

#### Development (`appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "ApplicationInsights": ""
  },
  "OpenTelemetry": {
    "TraceConfig": {
      "Sampler": "AlwaysOn"
    }
  }
}
```

#### Production (`appsettings.Production.json`):
```json
{
  "OpenTelemetry": {
    "TraceConfig": {
      "Sampler": "ParentBased(TraceIdRatio(0.05))"
    },
    "ResourceAttributes": {
      "deployment.environment": "production"
    }
  }
}
```

### Key Vault Integration

Store sensitive configuration in Azure Key Vault:

```bash
# Store Application Insights connection string
az keyvault secret set \
  --vault-name "marain-kv" \
  --name "ApplicationInsights-ConnectionString" \
  --value "$APPINSIGHTS_CONNECTION_STRING"
```

Reference in application:
```json
{
  "ConnectionStrings": {
    "ApplicationInsights": "@Microsoft.KeyVault(SecretUri=https://marain-kv.vault.azure.net/secrets/ApplicationInsights-ConnectionString/)"
  }
}
```

## Monitoring Setup

### 1. Import Dashboards

```bash
# Import tenant operations dashboard
az portal dashboard import \
  --input-path "./monitoring/dashboards/tenant-operations-dashboard.json" \
  --resource-group "marain-tenancy-rg"
```

### 2. Configure Live Metrics

Enable live metrics for real-time monitoring:

```bash
az monitor app-insights component update \
  --app "marain-tenancy-insights" \
  --resource-group "marain-tenancy-rg" \
  --set "publicNetworkAccessForIngestion=Enabled" \
       "publicNetworkAccessForQuery=Enabled"
```

### 3. Set Up Availability Tests

```bash
# Create availability test for health endpoint
az monitor app-insights web-test create \
  --resource-group "marain-tenancy-rg" \
  --name "marain-tenancy-health-check" \
  --location "East US" \
  --app-insights-component "marain-tenancy-insights" \
  --web-test-kind "ping" \
  --locations "East US,West US,North Europe" \
  --frequency 300 \
  --timeout 30 \
  --url "https://marain-tenancy-api.azurewebsites.net/health"
```

## Operations Procedures

### Daily Operations

#### 1. Morning Health Check
```bash
# Check system health for last 24 hours
az monitor app-insights query \
  --app "marain-tenancy-insights" \
  --analytics-query "
    requests 
    | where timestamp >= ago(24h)
    | where name contains 'tenant'
    | summarize 
        TotalRequests = count(),
        SuccessRate = round(100.0 * countif(success == true) / count(), 2),
        AvgDuration = round(avg(duration), 2),
        P95Duration = round(percentile(duration, 95), 2)
  "
```

#### 2. Error Review
```bash
# Check for new exceptions
az monitor app-insights query \
  --app "marain-tenancy-insights" \
  --analytics-query "
    exceptions 
    | where timestamp >= ago(24h)
    | where customDimensions['service.name'] == 'Marain.Tenancy'
    | summarize count() by type, bin(timestamp, 1h)
    | order by timestamp desc
  "
```

### Weekly Operations

#### 1. Performance Review
```bash
# Generate weekly performance report
az monitor app-insights query \
  --app "marain-tenancy-insights" \
  --analytics-query "
    requests
    | where timestamp >= ago(7d)
    | where name contains 'tenant'
    | summarize 
        DailyRequests = count(),
        AvgResponseTime = avg(duration),
        P95ResponseTime = percentile(duration, 95)
        by bin(timestamp, 1d)
    | order by timestamp desc
  "
```

#### 2. Cost Analysis
```bash
# Check Application Insights data volume
az monitor app-insights query \
  --app "marain-tenancy-insights" \
  --analytics-query "
    union traces, requests, dependencies, exceptions
    | where timestamp >= ago(7d)
    | summarize DataVolumeMB = sum(estimate_data_size()) / (1024*1024)
        by bin(timestamp, 1d)
    | order by timestamp desc
  "
```

### Monthly Operations

#### 1. Capacity Planning
- Review telemetry data growth trends
- Assess sampling rate effectiveness
- Plan for Application Insights capacity
- Review alert threshold effectiveness

#### 2. Performance Baseline Updates
- Update performance benchmarks
- Adjust alert thresholds based on trends
- Review telemetry overhead impact
- Update documentation with findings

## Troubleshooting

### Common Issues

#### 1. No Telemetry Data Appearing

**Symptoms**: No traces, metrics, or logs in Application Insights

**Diagnosis**:
```bash
# Check application logs for telemetry errors
az webapp log tail --name "marain-tenancy-api" --resource-group "marain-tenancy-rg"

# Verify configuration
az webapp config appsettings list --name "marain-tenancy-api" --resource-group "marain-tenancy-rg" \
  | grep -i "applicationinsights"
```

**Solutions**:
- Verify Application Insights connection string
- Check firewall rules and network connectivity
- Validate service configuration
- Review sampling settings

#### 2. High Telemetry Costs

**Symptoms**: Unexpected Application Insights charges

**Diagnosis**:
```bash
# Check daily data volume
az monitor app-insights query \
  --app "marain-tenancy-insights" \
  --analytics-query "
    usage 
    | where TimeGenerated >= ago(7d)
    | summarize DataVolume = sum(Quantity) by bin(TimeGenerated, 1d)
    | order by TimeGenerated desc
  "
```

**Solutions**:
- Implement more aggressive sampling
- Filter out high-volume, low-value data
- Set up daily cap on Application Insights
- Review and optimize telemetry patterns

#### 3. Performance Degradation

**Symptoms**: Increased response times after telemetry deployment

**Diagnosis**:
```bash
# Run performance benchmarks
dotnet run -c Release --project Solutions/Marain.Tenancy.PerformanceTests/ \
  -- --filter "*TelemetryOverheadBenchmarks*"
```

**Solutions**:
- Reduce sampling rates
- Optimize telemetry creation patterns
- Review export batch configurations
- Monitor memory usage and GC pressure

### Emergency Procedures

#### 1. Disable Telemetry Quickly
```bash
# Remove Application Insights connection string
az webapp config appsettings delete \
  --name "marain-tenancy-api" \
  --resource-group "marain-tenancy-rg" \
  --setting-names "ConnectionStrings__ApplicationInsights"

# Restart application
az webapp restart --name "marain-tenancy-api" --resource-group "marain-tenancy-rg"
```

#### 2. Emergency Sampling Rate Reduction
```bash
# Update sampling configuration via environment variable
az webapp config appsettings set \
  --name "marain-tenancy-api" \
  --resource-group "marain-tenancy-rg" \
  --settings "OpenTelemetry__TraceConfig__Sampler=ParentBased(TraceIdRatio(0.01))"
```

## Maintenance

### Regular Maintenance Tasks

#### Weekly
- Review alert noise and adjust thresholds
- Check telemetry data quality
- Monitor cost trends
- Review performance impact

#### Monthly
- Update performance baselines
- Review and optimize queries
- Assess sampling effectiveness
- Update documentation

#### Quarterly
- Review telemetry architecture
- Assess new OpenTelemetry features
- Update monitoring strategy
- Plan capacity changes

### Upgrade Procedures

#### 1. OpenTelemetry Package Updates
```bash
# Check current versions
dotnet list package --include-transitive | grep -i "opentelemetry"

# Update packages in order
dotnet add package OpenTelemetry --version [new-version]
dotnet add package Azure.Monitor.OpenTelemetry.Exporter --version [new-version]

# Test thoroughly before deployment
dotnet test --filter "Category=Telemetry"
```

#### 2. Application Insights Schema Changes
- Review breaking changes in Application Insights
- Update queries and dashboards
- Test alert rules with new schema
- Update documentation

### Backup and Disaster Recovery

#### Configuration Backup
```bash
# Export dashboard configurations
az portal dashboard show --name "tenant-operations-dashboard" > dashboard-backup.json

# Export alert rule configurations  
az monitor scheduled-query list --resource-group "marain-tenancy-rg" > alerts-backup.json
```

#### Recovery Procedures
1. Redeploy Application Insights instance
2. Restore dashboard configurations
3. Recreate alert rules
4. Update application connection strings
5. Validate telemetry flow

## Security Considerations

### Data Protection
- Ensure no PII in telemetry tags
- Use secure connection strings
- Implement least-privilege access
- Regular security reviews

### Compliance
- Document data retention policies
- Ensure GDPR compliance for EU data
- Implement data purging procedures
- Regular compliance audits

### Access Control
- Use Azure RBAC for Application Insights access
- Implement separate workspaces for environments
- Regular access reviews
- Secure API key management

---

**Last Updated**: 2025-01-15  
**Version**: 1.0  
**Next Review**: 2025-04-15