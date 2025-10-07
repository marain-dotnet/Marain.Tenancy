# Marain.Tenancy Telemetry Implementation Guide

This document provides comprehensive guidance for implementing, configuring, and monitoring OpenTelemetry-based observability in the Marain.Tenancy solution.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Configuration](#configuration)
- [Implementation Patterns](#implementation-patterns)
- [Testing](#testing)
- [Performance](#performance)
- [Monitoring & Alerting](#monitoring--alerting)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

## Overview

The Marain.Tenancy solution implements comprehensive observability using OpenTelemetry with Azure Monitor Application Insights integration. This provides end-to-end distributed tracing, custom metrics, and structured logging across all components.

### Key Benefits

- **End-to-end Visibility**: Complete request traces from CLI commands through API calls to storage operations
- **Performance Monitoring**: Real-time performance metrics with <5% overhead
- **Error Tracking**: Comprehensive exception capture and correlation
- **Business Metrics**: Custom tenant operation metrics and SLAs
- **Production Ready**: Enterprise-grade monitoring with Azure integration

### Components Covered

- **API Layer**: ASP.NET Core Minimal APIs (`Marain.Tenancy.Api`)
- **CLI Application**: Spectre.Console commands (`Marain.Tenancy.Cli`)
- **Client SDK**: HTTP client library (`Marain.Tenancy.Client`)
- **Business Logic**: Tenant providers and stores (`Marain.Tenancy.ClientTenantProvider`)
- **Storage Layer**: Azure Blob Storage operations (`Marain.Tenancy.Storage.Azure.BlobStorage`)

## Architecture

### Three-Signal Telemetry Strategy

#### 1. Traces (Distributed Tracing)
- **Automatic Instrumentation**: HTTP requests, Azure SDK calls, SQL operations
- **Custom Business Spans**: Tenant operations, workflow steps
- **Cross-Service Correlation**: W3C Trace Context propagation

#### 2. Metrics (Performance & Business KPIs)  
- **System Metrics**: Request rate, duration, error rate, memory usage
- **Business Metrics**: Tenant operations count, storage utilization
- **Custom Histograms**: Operation duration distributions

#### 3. Logs (Structured Logging)
- **Microsoft.Extensions.Logging**: Integrated with OpenTelemetry
- **Automatic Correlation**: Logs linked to traces and spans
- **Structured JSON**: Searchable properties and context

### Activity Sources & Meters

All telemetry components are defined in `TelemetryConstants`:

```csharp
public static class TelemetryConstants
{
    public const string ServiceName = "Marain.Tenancy";
    public const string ServiceVersion = "1.0.0";
    
    // Activity Sources (one per logical component)
    public const string ApiActivitySource = "Marain.Tenancy.Api";
    public const string CliActivitySource = "Marain.Tenancy.Cli";
    public const string ClientActivitySource = "Marain.Tenancy.Client";
    public const string BusinessActivitySource = "Marain.Tenancy.Business";
    public const string StorageActivitySource = "Marain.Tenancy.Storage";
    
    // Meters for custom metrics
    public const string TenancyMeter = "Marain.Tenancy";
}
```

## Configuration

### Application Settings

Configure telemetry in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "ApplicationInsights": "InstrumentationKey=your-key;IngestionEndpoint=https://your-endpoint"
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
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Marain.Tenancy": "Debug"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

### Environment-Specific Configuration

#### Development
```json
{
  "OpenTelemetry": {
    "TraceConfig": {
      "Sampler": "AlwaysOn"
    }
  }
}
```

#### Production
```json
{
  "OpenTelemetry": {
    "TraceConfig": {
      "Sampler": "ParentBased(TraceIdRatio(0.05))"
    }
  }
}
```

### Service Registration

Use the `AddMarainTelemetry` extension method:

```csharp
builder.Services.AddMarainTelemetry(builder.Configuration, builder.Environment);
```

This automatically configures:
- OpenTelemetry with Azure Monitor exporters
- Activity sources for all components
- Metrics collection and export
- Console exporters for development
- Resource attributes and sampling

## Implementation Patterns

### Activity Creation Pattern

```csharp
private static readonly ActivitySource ActivitySource = new(TelemetryConstants.BusinessActivitySource);

public async Task<ITenant> CreateChildTenantAsync(string parentId, Guid childGuid, string name)
{
    using Activity? activity = ActivitySource.StartActivity("tenant.create-child");
    activity?.SetTenantOperationTags(OperationTypes.Create, parentId);
    activity?.SetTag(AttributeKeys.ChildTenantGuid, childGuid.ToString());
    activity?.SetTag(AttributeKeys.TenantName, name);
    
    try
    {
        ITenant result = await this.CreateChildTenantInternalAsync(parentId, childGuid, name);
        activity?.SetStatus(ActivityStatusCode.Ok);
        return result;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        throw;
    }
}
```

### Metrics Recording Pattern

```csharp
private static readonly Meter Meter = new(TelemetryConstants.TenancyMeter);
private static readonly Counter<long> OperationCounter = 
    Meter.CreateCounter<long>("tenant.operations.total", "operations", "Total tenant operations");
private static readonly Histogram<double> OperationDuration = 
    Meter.CreateHistogram<double>("tenant.operation.duration", "ms", "Tenant operation duration");

public async Task ExecuteOperationAsync()
{
    using var timer = OperationDuration.Record();
    
    try
    {
        await DoWorkAsync();
        OperationCounter.Add(1, 
            new KeyValuePair<string, object?>("operation", "create"),
            new KeyValuePair<string, object?>("status", "success"));
    }
    catch (Exception)
    {
        OperationCounter.Add(1,
            new KeyValuePair<string, object?>("operation", "create"),
            new KeyValuePair<string, object?>("status", "error"));
        throw;
    }
}
```

### Error Handling Pattern

```csharp
public async Task<T> ExecuteWithTelemetryAsync<T>(Func<Task<T>> operation, string operationName)
{
    using Activity? activity = ActivitySource.StartActivity(operationName);
    
    try
    {
        T result = await operation();
        activity?.SetStatus(ActivityStatusCode.Ok);
        return result;
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.SetTag("error.type", ex.GetType().Name);
        activity?.SetTag("error.stack", ex.StackTrace);
        throw;
    }
}
```

## Testing

### Unit Testing with Telemetry Validation

```csharp
[Test]
public async Task Operation_ShouldCreateTelemetry()
{
    using TelemetryTestScope scope = TelemetryTestHelpers.CreateTelemetryTestScope();
    
    // Act
    await systemUnderTest.ExecuteOperationAsync("tenant-123");
    
    // Assert
    await scope.WaitForActivitiesAsync(1);
    Activity activity = scope.ValidateActivity(
        "operation-name",
        expectedTags: new Dictionary<string, object?>
        {
            ["tenant.id"] = "tenant-123"
        },
        expectedStatus: ActivityStatusCode.Ok);
}
```

### Performance Testing

Run comprehensive performance benchmarks:

```bash
# Full performance test suite
dotnet run -c Release --project Solutions/Marain.Tenancy.PerformanceTests/

# Specific benchmark
dotnet run -c Release --project Solutions/Marain.Tenancy.PerformanceTests/ -- --filter "*TelemetryOverheadBenchmarks*"
```

Expected results:
- **Telemetry Overhead**: <5% for critical operations
- **Memory Impact**: <20% additional allocations
- **Concurrency**: Linear scaling with available resources

### Integration Testing

```csharp
[Test]
public async Task EndToEndFlow_ShouldMaintainTraceCorrelation()
{
    using Activity? parentActivity = Activity.StartActivity("e2e-test");
    
    // CLI → Client → API → Storage
    await cliCommand.ExecuteAsync();
    
    await telemetryScope.WaitForActivitiesAsync(4);
    var activities = telemetryScope.CapturedActivities;
    
    // Verify trace correlation
    Assert.That(activities.All(a => a.TraceId == parentActivity.TraceId));
}
```

## Performance

### Sampling Configuration

#### Development Environment
- **100% sampling** for complete visibility during development
- All activities and metrics recorded

#### Production Environment  
- **5-10% sampling** for production workloads
- Adaptive sampling based on volume
- Critical errors always sampled

### Resource Optimization

```csharp
services.Configure<BatchExportActivityProcessorOptions>(options =>
{
    options.MaxExportBatchSize = 512;
    options.ScheduledDelayMilliseconds = 1000;
    options.ExporterTimeoutMilliseconds = 30000;
    options.MaxQueueSize = 2048;
});
```

### Performance Monitoring

Monitor these key metrics:
- **Telemetry Overhead**: <5% performance impact
- **Memory Usage**: Monitor GC pressure and allocation rates  
- **Export Buffer**: Track queue depths and export success rates
- **Sampling Effectiveness**: Verify appropriate sampling rates

## Monitoring & Alerting

### Application Insights Dashboards

#### Tenant Operations Dashboard
- Request rates and success rates
- Operation duration trends
- Error rates by operation type
- Top failing tenants

#### Performance Dashboard
- Response time percentiles
- Throughput metrics
- Resource utilization
- Telemetry system health

#### Business Metrics Dashboard
- Tenant creation rates
- Storage utilization
- SLA compliance metrics
- Custom business KPIs

### Alert Configuration

#### Performance Alerts
```
Metric: requests/duration
Condition: 95th percentile > 5000ms
Frequency: 5 minutes
Actions: Email, Teams notification
```

#### Error Rate Alerts
```
Metric: requests/failed
Condition: Failure rate > 5%
Frequency: 1 minute
Actions: Page operations team
```

#### Availability Alerts
```
Metric: availabilityResults/availabilityPercentage
Condition: Availability < 99%
Frequency: 5 minutes
Actions: Escalate to on-call
```

### KQL Queries

#### Find slow operations:
```kql
traces
| where timestamp >= ago(1h)
| where customDimensions.["operation.type"] == "create"
| where duration > 5000
| summarize count() by tostring(customDimensions.["tenant.id"])
| top 10 by count_
```

#### Error analysis:
```kql
exceptions
| where timestamp >= ago(24h)
| where customDimensions.["service.name"] == "Marain.Tenancy"
| summarize count() by type, tostring(customDimensions.["operation.type"])
| order by count_ desc
```

## Troubleshooting

### Common Issues

#### 1. Missing Activities
**Symptoms**: Activities not appearing in Application Insights
**Solutions**:
- Verify ActivitySource names match TelemetryConstants
- Check Application Insights connection string
- Confirm sampling configuration
- Review export success metrics

#### 2. Performance Degradation
**Symptoms**: Increased response times after telemetry enablement
**Solutions**:
- Reduce sampling rates in production
- Optimize activity creation patterns
- Review export batch configuration
- Monitor memory usage and GC pressure

#### 3. Correlation Issues
**Symptoms**: Broken trace correlation across components
**Solutions**:
- Verify W3C Trace Context headers
- Check Activity.Current propagation
- Review HTTP client instrumentation
- Validate parent-child relationships

#### 4. High Telemetry Costs
**Symptoms**: Unexpected Application Insights charges
**Solutions**:
- Implement appropriate sampling rates
- Set up data retention policies
- Filter out health check requests
- Monitor ingestion volumes

### Diagnostic Commands

```bash
# Check telemetry configuration
dotnet run --project Solutions/Marain.Tenancy.Api/ -- --validate-telemetry

# Test performance with telemetry
dotnet run -c Release --project Solutions/Marain.Tenancy.PerformanceTests/

# Verify trace correlation
dotnet test --filter "Category=Integration&Category=Telemetry"
```

### Debugging Techniques

1. **Enable Console Exporter**: Use console output for local debugging
2. **Activity Inspection**: Use debugger to inspect Activity.Current
3. **Trace Analysis**: Review complete trace flows in Application Insights
4. **Performance Profiling**: Use dotTrace or PerfView for detailed analysis

## Best Practices

### Development Guidelines

1. **Consistent Naming**: Use TelemetryConstants for all activity sources and meters
2. **Error Handling**: Always set error status on activities for exceptions
3. **Tag Standardization**: Use AttributeKeys constants for consistent tag names
4. **Resource Cleanup**: Properly dispose activities and meters
5. **Testing Coverage**: Include telemetry validation in all tests

### Production Considerations

1. **Sampling Strategy**: Implement appropriate sampling for production volumes
2. **Performance Monitoring**: Continuously monitor telemetry overhead
3. **Cost Management**: Set up alerts for Application Insights costs
4. **Data Retention**: Configure appropriate retention policies
5. **Privacy Compliance**: Ensure no PII in telemetry data

### Security Guidelines

1. **No Sensitive Data**: Never include passwords, keys, or PII in tags
2. **Connection Security**: Use secure connection strings and endpoints
3. **Access Control**: Restrict Application Insights workspace access
4. **Data Sovereignty**: Consider data location requirements
5. **Compliance**: Follow organizational compliance requirements

### Maintenance Procedures

1. **Regular Reviews**: Quarterly telemetry data and cost reviews
2. **Performance Baselines**: Maintain performance baselines for regression detection
3. **Dashboard Updates**: Keep dashboards current with business needs
4. **Alert Tuning**: Regularly tune alert thresholds to reduce noise
5. **Documentation**: Keep implementation documentation current

## Additional Resources

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Azure Monitor Application Insights](https://docs.microsoft.com/azure/azure-monitor/app/)
- [Performance Testing Guide](../testing/performance-testing-guide.md)
- [Monitoring Runbooks](../operations/monitoring-runbooks.md)
- [Alert Configuration Templates](../monitoring/alert-templates/)

---

**Last Updated**: 2025-01-15  
**Version**: 1.0  
**Next Review**: 2025-04-15