# Marain.Tenancy Application Insights KQL Queries

This document contains common KQL queries for monitoring the Marain.Tenancy service in Application Insights.

## Performance Monitoring

### Operation Duration Percentiles
```kusto
customMetrics
| where name contains "operation.duration"
| summarize 
    P50 = percentile(value, 50),
    P95 = percentile(value, 95), 
    P99 = percentile(value, 99),
    Count = count()
    by name, bin(timestamp, 5m)
| order by timestamp desc
```

### Slow Operations (P99 > 1000ms)
```kusto
customMetrics
| where name contains "operation.duration"
| summarize P99 = percentile(value, 99) by bin(timestamp, 5m)
| where P99 > 1000
| order by timestamp desc
```

### Request Rate and Success Rate
```kusto
requests
| where name contains "tenant"
| summarize 
    RequestRate = count()/bin(5m),
    SuccessRate = (countif(success == true) * 100.0) / count(),
    AvgDuration = avg(duration)
    by bin(timestamp, 5m)
| order by timestamp desc
```

## Error Monitoring

### Error Rate by Operation Type
```kusto
customMetrics
| where name contains "errors.total"
| extend OperationType = tostring(customDimensions["operation.type"])
| extend ErrorType = tostring(customDimensions["error.type"])
| summarize ErrorCount = sum(value) by OperationType, ErrorType, bin(timestamp, 10m)
| join kind=leftouter (
    customMetrics
    | where name contains "operations.total"
    | extend OperationType = tostring(customDimensions["operation.type"])
    | summarize TotalOps = sum(value) by OperationType, bin(timestamp, 10m)
) on OperationType, timestamp
| extend ErrorRate = (ErrorCount * 100.0) / TotalOps
| order by timestamp desc, ErrorRate desc
```

### Top Error Messages
```kusto
exceptions
| extend TenantId = tostring(customDimensions["tenant.id"])
| extend OperationType = tostring(customDimensions["operation.type"])
| summarize 
    Count = count(),
    SampleMessage = any(outerMessage),
    AffectedTenants = dcount(TenantId)
    by type, OperationType
| order by Count desc
```

### Failed Tenant Operations
```kusto
traces
| where message contains "Failed" and severityLevel >= 3
| extend TenantId = tostring(customDimensions["tenant.id"])
| extend OperationType = tostring(customDimensions["operation.type"])
| summarize FailureCount = count() by TenantId, OperationType, bin(timestamp, 15m)
| order by timestamp desc, FailureCount desc
```

## Business Metrics

### Tenant Creation Rate
```kusto
customMetrics
| where name == "tenant.operations.total" or name == "tenant.storage.operations.total" or name == "tenant.business.operations.total"
| where customDimensions["operation.type"] == "create"
| where customDimensions["status"] == "success"
| summarize CreationRate = sum(value) by bin(timestamp, 1h)
| order by timestamp desc
```

### Most Active Tenants
```kusto
customMetrics
| where name contains "operations.total"
| extend TenantId = tostring(customDimensions["tenant.id"])
| where isnotempty(TenantId)
| summarize 
    TotalOperations = sum(value),
    OperationTypes = dcount(tostring(customDimensions["operation.type"]))
    by TenantId
| order by TotalOperations desc
| limit 50
```

### Storage Utilization by Tenant
```kusto
customMetrics
| where name == "tenant.storage.blob.size"
| extend TenantId = tostring(customDimensions["tenant.id"])
| extend Container = tostring(customDimensions["azure.container"])
| summarize 
    TotalSize = sum(value),
    BlobCount = count(),
    AvgBlobSize = avg(value)
    by TenantId, Container
| order by TotalSize desc
```

## Health and SLA Monitoring

### Service Health Score (Success Rate)
```kusto
let successRate = customMetrics
| where name contains "operations.total"
| extend Status = tostring(customDimensions["status"])
| summarize 
    SuccessOps = sumif(value, Status == "success"),
    TotalOps = sum(value)
| extend HealthScore = (SuccessOps * 100.0) / TotalOps;
successRate
| project HealthScore, Timestamp = now()
```

### SLA Breach Detection (P95 > 2000ms)
```kusto
customMetrics
| where name contains "operation.duration"
| summarize P95Duration = percentile(value, 95) by bin(timestamp, 5m)
| where P95Duration > 2000  // SLA breach threshold
| extend SLABreach = "P95 latency exceeded 2000ms"
| order by timestamp desc
```

### Critical Error Alerts
```kusto
union exceptions, traces
| where timestamp > ago(5m)
| where severityLevel >= 4  // Critical errors only
| extend TenantId = tostring(customDimensions["tenant.id"])
| extend OperationType = tostring(customDimensions["operation.type"])
| summarize 
    Count = count(),
    AffectedTenants = dcount(TenantId),
    SampleError = any(message)
    by OperationType
| where Count > 0
```

## Capacity Planning

### Operation Volume Trends
```kusto
customMetrics
| where name contains "operations.total"
| extend OperationType = tostring(customDimensions["operation.type"])
| summarize OperationVolume = sum(value) by OperationType, bin(timestamp, 1h)
| order by timestamp desc
```

### Peak Usage Analysis
```kusto
customMetrics
| where name contains "operations.total"
| summarize HourlyOperations = sum(value) by bin(timestamp, 1h)
| summarize 
    PeakOperations = max(HourlyOperations),
    AvgOperations = avg(HourlyOperations),
    PeakToAvgRatio = max(HourlyOperations) / avg(HourlyOperations)
```

### Storage Growth Rate
```kusto
customMetrics
| where name == "tenant.storage.blob.size"
| summarize TotalStorageBytes = sum(value) by bin(timestamp, 1d)
| sort by timestamp asc
| extend StorageGB = TotalStorageBytes / (1024*1024*1024)
| extend GrowthRate = StorageGB - prev(StorageGB)
| where isnotempty(GrowthRate)
| order by timestamp desc
```

## Advanced Troubleshooting

### Correlation Analysis (Request to Storage)
```kusto
requests
| where name contains "tenant"
| extend TenantId = tostring(customDimensions["tenant.id"])
| join kind=inner (
    dependencies
    | where target contains "blob" or target contains "storage"
    | extend TenantId = tostring(customDimensions["tenant.id"])
) on operation_Id, TenantId
| project timestamp, TenantId, RequestDuration = duration, StorageDuration = duration1, RequestSuccess = success, StorageSuccess = success1
| order by timestamp desc
```

### End-to-End Transaction Tracing
```kusto
requests
| where operation_Id == "specific-operation-id"  // Replace with actual operation ID
| union (dependencies | where operation_Id == "specific-operation-id")
| union (traces | where operation_Id == "specific-operation-id")
| union (exceptions | where operation_Id == "specific-operation-id")
| order by timestamp asc
```

### Performance Regression Detection
```kusto
let currentPeriod = customMetrics
| where timestamp > ago(1h)
| where name contains "operation.duration"
| summarize CurrentP95 = percentile(value, 95) by name;
let previousPeriod = customMetrics
| where timestamp between (ago(2h) .. ago(1h))
| where name contains "operation.duration"
| summarize PreviousP95 = percentile(value, 95) by name;
currentPeriod
| join kind=inner previousPeriod on name
| extend RegressionRatio = CurrentP95 / PreviousP95
| where RegressionRatio > 1.5  // 50% regression threshold
| order by RegressionRatio desc
```

## Usage Notes

1. Replace placeholder values like `{subscription-id}`, `{resource-group}`, and `{app-insights-name}` with actual values
2. Adjust time ranges and thresholds based on your specific SLA requirements
3. Create alerts based on these queries for proactive monitoring
4. Use these queries as templates for custom dashboards and workbooks
5. Monitor query performance and optimize for your data volume

## Alert Recommendations

- **High Error Rate**: Error rate > 5% over 5-minute window
- **High Latency**: P95 duration > 2000ms over 5-minute window  
- **Service Unavailable**: No successful operations in 10 minutes
- **Storage Growth**: Storage growth > 10GB per day
- **Critical Errors**: Any critical level exceptions or traces