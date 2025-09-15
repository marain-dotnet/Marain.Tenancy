# Marain.Tenancy Monitoring Setup

This directory contains the monitoring infrastructure and configuration for the Marain.Tenancy service using Azure Application Insights and Log Analytics.

## Overview

The monitoring setup provides:
- **Comprehensive telemetry collection** via OpenTelemetry
- **Real-time dashboards** for operational insights
- **Proactive alerting** for critical issues
- **Performance monitoring** and SLA tracking
- **Business metrics** and usage analytics

## Quick Start

### 1. Deploy Infrastructure

```bash
# Make the script executable (Linux/macOS)
chmod +x deploy-monitoring.ps1

# Deploy for production
./deploy-monitoring.ps1 \
  -ResourceGroupName "rg-marain-tenancy-prod" \
  -ApplicationInsightsName "ai-marain-tenancy-prod" \
  -LogAnalyticsWorkspaceName "law-marain-tenancy-prod" \
  -Environment "prod" \
  -AlertEmailAddresses "admin@company.com,devops@company.com"

# Deploy for development
./deploy-monitoring.ps1 \
  -ResourceGroupName "rg-marain-tenancy-dev" \
  -ApplicationInsightsName "ai-marain-tenancy-dev" \
  -LogAnalyticsWorkspaceName "law-marain-tenancy-dev" \
  -Environment "dev"
```

### 2. Configure Application

Update your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "ApplicationInsights": "InstrumentationKey=...;IngestionEndpoint=..."
  },
  "ApplicationInsights": {
    "EnableAdaptiveSampling": true,
    "SamplingSettings": {
      "SamplingPercentage": 100
    }
  },
  "OpenTelemetry": {
    "TraceConfig": {
      "Sampler": "ParentBased(TraceIdRatio(0.1))",
      "MaxAttributes": 32,
      "MaxEvents": 128
    },
    "ResourceAttributes": {
      "service.name": "Marain.Tenancy",
      "service.version": "1.0.0",
      "deployment.environment": "production"
    }
  }
}
```

### 3. Import Dashboard

1. Update `application-insights-dashboard.json` with your Application Insights resource ID
2. Import via Azure CLI:
   ```bash
   az portal dashboard import --input-path monitoring/application-insights-dashboard.json --resource-group your-resource-group
   ```

## Files Description

### Infrastructure as Code
- **`application-insights-setup.bicep`** - Bicep template for Azure resources
- **`deploy-monitoring.ps1`** - PowerShell deployment script

### Monitoring Configuration  
- **`application-insights-dashboard.json`** - Azure Portal dashboard definition
- **`kql-queries.md`** - Collection of useful KQL queries for monitoring

### Documentation
- **`README.md`** - This file

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Application   │───▶│  OpenTelemetry   │───▶│ App Insights    │
│   (API/CLI)     │    │   Exporters      │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                         │
┌─────────────────┐    ┌──────────────────┐             │
│    Storage      │───▶│   Activities &   │             │
│    Layer        │    │     Metrics      │             │
└─────────────────┘    └──────────────────┘             │
                                                         ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Business      │───▶│     Logging      │───▶│ Log Analytics   │
│   Logic         │    │   Integration    │    │   Workspace     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                         │
                                                         ▼
                                               ┌─────────────────┐
                                               │   Dashboards    │
                                               │   & Alerts      │
                                               └─────────────────┘
```

## Telemetry Data Collected

### Custom Metrics
- `tenant.operations.total` - Total tenant operations by type and status
- `tenant.operation.duration` - Operation duration histograms
- `tenant.storage.operations.total` - Storage operation counters
- `tenant.storage.operation.duration` - Storage operation durations
- `tenant.storage.blob.size` - Tenant blob sizes for capacity planning
- `tenant.business.operations.total` - Business logic operation counters
- `tenant.business.operation.duration` - Business operation durations
- `tenant.*.errors.total` - Error counters by category

### Traces
- Structured logs with tenant context
- Operation correlation IDs
- Error details and stack traces
- Performance markers

### Dependencies
- Azure Blob Storage calls
- HTTP client requests
- Database operations (if applicable)

## Alert Rules

The deployment creates the following alert rules:

### Critical Alerts (Severity 0-1)
- **Critical Errors** - Any critical level exceptions
- **Service Unavailable** - No successful operations for 10+ minutes

### Warning Alerts (Severity 2-3)  
- **High Error Rate** - Error rate > 5% over 5 minutes
- **High Latency** - P95 duration > 2000ms over 5 minutes
- **Storage Growth** - Storage growth > 10GB per day

## Dashboard Metrics

The operational dashboard includes:

### Performance Monitoring
- Request rates and success rates
- Operation duration percentiles (P50, P95, P99)
- Error rates by operation type
- Top slowest operations

### Business Intelligence
- Tenant creation rates
- Most active tenants
- Storage utilization by tenant
- Operation volume trends

### Health & SLA Tracking
- Service health score
- SLA breach detection
- Availability metrics
- Error distribution

## KQL Query Examples

### Top Error Types
```kusto
customMetrics
| where name contains "errors.total"
| extend ErrorType = tostring(customDimensions["error.type"])
| summarize TotalErrors = sum(value) by ErrorType
| order by TotalErrors desc
```

### Performance Trends
```kusto
customMetrics
| where name contains "operation.duration"
| summarize P95 = percentile(value, 95) by bin(timestamp, 5m)
| render timechart
```

### Most Active Tenants
```kusto
customMetrics
| where name contains "operations.total"
| extend TenantId = tostring(customDimensions["tenant.id"])
| where isnotempty(TenantId)
| summarize Operations = sum(value) by TenantId
| order by Operations desc
| limit 20
```

## Environment-Specific Configuration

### Development
- 100% sampling for full visibility
- Lower alert thresholds for faster feedback
- Extended data retention for debugging

### Staging  
- 50% sampling for comprehensive testing
- Production-like alert thresholds
- Standard data retention

### Production
- 10% sampling (configurable based on traffic)
- Strict SLA-based alert thresholds
- Cost-optimized retention policies

## Troubleshooting

### Common Issues

1. **No telemetry data**
   - Verify Application Insights connection string
   - Check firewall/network connectivity
   - Validate OpenTelemetry configuration

2. **High data volume costs**
   - Reduce sampling percentage
   - Adjust daily data cap
   - Review and filter unnecessary telemetry

3. **False positive alerts**
   - Adjust alert thresholds
   - Increase evaluation windows
   - Add exclusion filters

### Debugging Commands

```bash
# Check Application Insights connectivity
az monitor app-insights component show --app ai-marain-tenancy-prod --resource-group rg-marain-tenancy-prod

# View recent telemetry
az monitor app-insights query --app ai-marain-tenancy-prod --analytics-query "requests | limit 10"

# Test alert rules
az monitor metrics alert list --resource-group rg-marain-tenancy-prod
```

## Cost Optimization

### Data Volume Management
- Use sampling strategies appropriate to environment
- Set daily data caps to prevent cost overruns
- Regularly review data retention policies
- Filter out high-volume, low-value telemetry

### Query Optimization
- Use time-based filters in KQL queries
- Limit result sets with `limit` clause
- Cache frequently-used query results
- Use aggregated data for dashboards

## Security Considerations

- Application Insights data contains tenant IDs - ensure proper access controls
- Use managed identities where possible
- Regularly audit access to monitoring data
- Consider data residency requirements for tenant data

## Support and Maintenance

### Regular Tasks
- Review and update alert thresholds monthly
- Archive old dashboard configurations
- Validate backup and retention policies
- Update KQL queries for new telemetry

### Monitoring the Monitoring
- Set up alerts on Application Insights health
- Monitor data ingestion volumes and costs  
- Track query performance and optimization opportunities
- Verify alert rule effectiveness

## Further Reading

- [Application Insights Overview](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [KQL Query Language](https://docs.microsoft.com/azure/data-explorer/kusto/query/)
- [Azure Monitor Best Practices](https://docs.microsoft.com/azure/azure-monitor/best-practices)