# ADR-001: OpenTelemetry Integration with Application Insights

## Status
**Proposed** - 2025-01-09

## Context

Marain.Tenancy is a multi-project .NET 8 solution providing tenant management functionality with the following architectural components:

- **API Layer**: ASP.NET Core Minimal APIs (`Marain.Tenancy.Api`)
- **CLI Application**: Console application using Spectre.Console (`Marain.Tenancy.Cli`) 
- **Client Libraries**: HTTP client SDK (`Marain.Tenancy.Client`)
- **Business Logic**: Tenant provider and store implementations (`Marain.Tenancy.ClientTenantProvider`)
- **Storage Layer**: Azure Blob Storage integration (`Marain.Tenancy.Storage.Azure.BlobStorage`)

### Current Observability Gaps

1. **Limited Visibility**: No end-to-end tracing across service boundaries
2. **Manual Correlation**: Difficult to correlate logs and errors across components  
3. **Performance Blind Spots**: No instrumentation of critical business operations
4. **Debugging Challenges**: Complex multi-layer architecture requires better observability
5. **Production Monitoring**: Need proactive monitoring and alerting capabilities

### Requirements

- **Distributed Tracing**: End-to-end visibility across all components
- **Application Performance Monitoring**: Method-level performance insights
- **Structured Logging**: Correlated, searchable logs with context
- **Custom Business Metrics**: Tenant operation metrics and SLAs
- **Error Tracking**: Comprehensive exception capture and analysis
- **Production Ready**: Minimal performance overhead, proper sampling
- **Azure Integration**: Native Application Insights integration

## Decision

We will implement **OpenTelemetry** with **Azure Monitor Application Insights** integration using a layered instrumentation approach.

### Core Architecture

#### 1. Three-Signal Telemetry Strategy

**Traces (Distributed Tracing)**
- Automatic instrumentation: HTTP, Azure SDK, SQL Client
- Custom business operation spans
- Cross-service correlation via W3C Trace Context

**Metrics (Performance & Business KPIs)**
- System metrics: Request rate, duration, error rate
- Business metrics: Tenant operations, storage utilization
- Custom counters and histograms

**Logs (Structured Logging)**
- Microsoft.Extensions.Logging integration
- Automatic trace correlation
- Structured JSON output with semantic properties

#### 2. Activity Sources & Meters

```csharp
// Shared telemetry constants
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

#### 3. Package Dependencies

```xml
<!-- Core OpenTelemetry packages -->
<PackageReference Include="OpenTelemetry" Version="1.7.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />

<!-- Instrumentation packages -->
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.7.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.7.1" />
<PackageReference Include="OpenTelemetry.Instrumentation.SqlClient" Version="1.7.1" />

<!-- Azure Monitor integration -->
<PackageReference Include="Azure.Monitor.OpenTelemetry.Exporter" Version="1.2.0" />
<PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="1.1.0" />
```

### Implementation Strategy

#### 1. API Layer Integration (`Marain.Tenancy.Api`)

**Program.cs Configuration:**
```csharp
// Configure OpenTelemetry with Application Insights
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(TelemetryConstants.ServiceName, TelemetryConstants.ServiceVersion))
    .WithTracing(tracing => tracing
        .AddSource(TelemetryConstants.ApiActivitySource)
        .AddSource(TelemetryConstants.BusinessActivitySource) 
        .AddSource(TelemetryConstants.StorageActivitySource)
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = httpContext => 
                !httpContext.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        .AddAzureMonitorTraceExporter())
    .WithMetrics(metrics => metrics
        .AddMeter(TelemetryConstants.TenancyMeter)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddAzureMonitorMetricExporter());

// Enhanced logging with Application Insights
builder.Services.AddApplicationInsightsTelemetry();
```

**Custom Endpoint Instrumentation:**
```csharp
// Extension method for consistent span creation
public static class TelemetryExtensions
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.ApiActivitySource);
    
    public static Activity? StartTenantOperation(this Activity? parent, 
        string operationName, string tenantId)
    {
        var activity = ActivitySource.StartActivity(operationName);
        activity?.SetTag("tenant.id", tenantId);
        activity?.SetTag("operation.type", "tenant");
        return activity;
    }
}
```

#### 2. CLI Application Integration (`Marain.Tenancy.Cli`)

**Program.cs Configuration:**
```csharp
builder.ConfigureServices((ctx, services) =>
{
    // Existing service configuration...
    
    // Add OpenTelemetry for CLI
    services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService($"{TelemetryConstants.ServiceName}.Cli"))
        .WithTracing(tracing => tracing
            .AddSource(TelemetryConstants.CliActivitySource)
            .AddHttpClientInstrumentation()
            .AddAzureMonitorTraceExporter());
    
    // Configure logging
    services.AddLogging(logging => logging
        .AddApplicationInsights()
        .AddConsole());
});
```

**Command Instrumentation:**
```csharp
public class Get(ITenantProvider tenantProvider, 
    IJsonSerializerOptionsProvider serializationSettingsProvider) : AsyncCommand<GetSettings>
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.CliActivitySource);
    private static readonly ILogger<Get> Logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<Get>();
    
    public override async Task<int> ExecuteAsync(CommandContext context, GetSettings settings)
    {
        using var activity = ActivitySource.StartActivity("cli.get-tenant");
        var tenantId = string.IsNullOrEmpty(settings.TenantId) 
            ? tenantProvider.Root.Id 
            : settings.TenantId;
            
        activity?.SetTag("tenant.id", tenantId);
        activity?.SetTag("command.type", "get");
        
        Logger.LogInformation("Getting tenant {TenantId}", tenantId);
        
        try
        {
            ITenant tenant = await tenantProvider.GetTenantAsync(tenantId);
            activity?.SetStatus(ActivityStatusCode.Ok);
            
            string result = JsonSerializer.Serialize(tenant, serializationSettingsProvider.Instance);
            AnsiConsole.WriteLine(result);
            
            Logger.LogInformation("Successfully retrieved tenant {TenantId}", tenantId);
            return 0;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Logger.LogError(ex, "Failed to get tenant {TenantId}", tenantId);
            throw;
        }
    }
}
```

#### 3. Business Logic Instrumentation

**Tenant Operations:**
```csharp
public class ClientTenantStore(/*...*/) : ClientTenantProvider(/*...*/, ITenantStore
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.BusinessActivitySource);
    private static readonly Meter Meter = new(TelemetryConstants.TenancyMeter);
    private static readonly Counter<long> TenantOperationsCounter = 
        Meter.CreateCounter<long>("tenant.operations.total", "operations", "Total tenant operations");
    private static readonly Histogram<double> TenantOperationDuration = 
        Meter.CreateHistogram<double>("tenant.operation.duration", "ms", "Tenant operation duration");
    
    private readonly ILogger<ClientTenantStore> logger;

    public async Task<ITenant> CreateWellKnownChildTenantAsync(
        string parentTenantId, Guid wellKnownChildTenantGuid, string name)
    {
        using var activity = ActivitySource.StartActivity("tenant.create-child");
        using var timer = TenantOperationDuration.Record();
        
        activity?.SetTag("tenant.parent.id", parentTenantId);
        activity?.SetTag("tenant.child.guid", wellKnownChildTenantGuid.ToString());
        activity?.SetTag("tenant.name", name);
        activity?.SetTag("operation.type", "create");
        
        logger.LogInformation(
            "Creating child tenant {ChildTenantGuid} with name {TenantName} under parent {ParentTenantId}",
            wellKnownChildTenantGuid, name, parentTenantId);
        
        try
        {
            var result = await this.CreateChildTenantAsync(parentTenantId, name, wellKnownChildTenantGuid);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            TenantOperationsCounter.Add(1, 
                new KeyValuePair<string, object?>("operation", "create"),
                new KeyValuePair<string, object?>("status", "success"));
                
            logger.LogInformation(
                "Successfully created child tenant {TenantId} with name {TenantName}",
                result.Id, name);
                
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            TenantOperationsCounter.Add(1,
                new KeyValuePair<string, object?>("operation", "create"),
                new KeyValuePair<string, object?>("status", "error"));
                
            logger.LogError(ex, 
                "Failed to create child tenant {ChildTenantGuid} under parent {ParentTenantId}",
                wellKnownChildTenantGuid, parentTenantId);
            throw;
        }
    }
}
```

#### 4. Storage Layer Instrumentation

**Azure Blob Storage Operations:**
```csharp
internal class AzureBlobStorageTenantStore(/*...*/) : ITenantStore
{
    private static readonly ActivitySource ActivitySource = new(TelemetryConstants.StorageActivitySource);
    private static readonly Counter<long> StorageOperationsCounter = 
        Meter.CreateCounter<long>("storage.operations.total", "operations", "Total storage operations");
    
    private readonly ILogger<AzureBlobStorageTenantStore> logger;

    public async Task<ITenant> CreateWellKnownChildTenantAsync(
        string parentTenantId, Guid wellKnownChildTenantGuid, string name)
    {
        using var activity = ActivitySource.StartActivity("storage.create-tenant");
        activity?.SetTag("storage.operation", "create");
        activity?.SetTag("tenant.parent.id", parentTenantId);
        activity?.SetTag("azure.container", "corvustenancy");
        
        try
        {
            // Existing implementation with added logging
            logger.LogDebug("Starting tenant creation in blob storage for parent {ParentTenantId}", parentTenantId);
            
            var (parentTenant, container) = await GetContainerAndTenantForChildTenantsOfAsync(parentTenantId);
            
            // Storage operation instrumentation continues...
            StorageOperationsCounter.Add(1,
                new KeyValuePair<string, object?>("operation", "create"),
                new KeyValuePair<string, object?>("status", "success"));
                
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            StorageOperationsCounter.Add(1,
                new KeyValuePair<string, object?>("operation", "create"),
                new KeyValuePair<string, object?>("status", "error"));
            throw;
        }
    }
}
```

#### 5. Configuration & Environment Management

**appsettings.json:**
```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...",
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
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Marain.Tenancy": "Debug",
      "Microsoft.AspNetCore": "Warning"
    },
    "ApplicationInsights": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

**Environment-Specific Configuration:**
```csharp
// Extension method for environment-specific telemetry setup
public static IServiceCollection AddMarainTelemetry(
    this IServiceCollection services, 
    IConfiguration configuration,
    IWebHostEnvironment? environment = null)
{
    var connectionString = configuration.GetConnectionString("ApplicationInsights");
    var isDevelopment = environment?.IsDevelopment() ?? false;
    
    services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(TelemetryConstants.ServiceName, TelemetryConstants.ServiceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = environment?.EnvironmentName ?? "unknown",
                ["service.instance.id"] = Environment.MachineName
            }))
        .WithTracing(tracing =>
        {
            tracing.AddSource(TelemetryConstants.ApiActivitySource)
                   .AddSource(TelemetryConstants.BusinessActivitySource)
                   .AddSource(TelemetryConstants.StorageActivitySource)
                   .AddAspNetCoreInstrumentation()
                   .AddHttpClientInstrumentation();
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                tracing.AddAzureMonitorTraceExporter();
            }
            else if (isDevelopment)
            {
                tracing.AddConsoleExporter();
            }
        })
        .WithMetrics(metrics =>
        {
            metrics.AddMeter(TelemetryConstants.TenancyMeter)
                   .AddAspNetCoreInstrumentation()
                   .AddRuntimeInstrumentation();
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                metrics.AddAzureMonitorMetricExporter();
            }
        });
    
    return services;
}
```

### Testing Strategy

#### 1. Unit Testing with Telemetry Validation

```csharp
[TestClass]
public class TenantOperationTelemetryTests
{
    private readonly TestActivityListener activityListener;
    private readonly List<Activity> recordedActivities;
    
    [TestInitialize]
    public void Setup()
    {
        recordedActivities = new List<Activity>();
        activityListener = new TestActivityListener(recordedActivities);
        ActivitySource.AddActivityListener(activityListener);
    }
    
    [TestMethod]
    public async Task CreateTenant_ShouldCreateSpanWithCorrectAttributes()
    {
        // Arrange
        var store = CreateTenantStore();
        
        // Act
        await store.CreateWellKnownChildTenantAsync("parent", Guid.NewGuid(), "test");
        
        // Assert
        var activity = recordedActivities.Single(a => a.DisplayName == "tenant.create-child");
        Assert.AreEqual("create", activity.GetTagItem("operation.type"));
        Assert.AreEqual(ActivityStatusCode.Ok, activity.Status);
    }
}
```

#### 2. Integration Testing with Application Insights

```csharp
[TestClass]
public class TelemetryIntegrationTests
{
    [TestMethod]
    public async Task ApiEndpoint_ShouldSendTelemetryToApplicationInsights()
    {
        // Arrange
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Configure test Application Insights
                    services.Configure<ApplicationInsightsServiceOptions>(options =>
                    {
                        options.ConnectionString = TestConfiguration.ApplicationInsightsConnectionString;
                    });
                });
            });
        
        var client = factory.CreateClient();
        
        // Act
        var response = await client.GetAsync("/tenants/test-tenant-id");
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        // Verify telemetry was sent (using Application Insights query API or test doubles)
        await VerifyTelemetryWasSent("tenant.get", "test-tenant-id");
    }
}
```

### Performance Considerations

#### 1. Sampling Strategy
- **Development**: 100% sampling for full visibility
- **Staging**: 50% sampling for comprehensive testing
- **Production**: 10% sampling (configurable based on traffic)

#### 2. Attribute Limits
- Maximum 32 attributes per span
- Maximum 128 events per span
- String attribute truncation at 1024 characters

#### 3. Async Operations
- All telemetry operations are non-blocking
- Background export to minimize latency impact
- Batch export configuration optimized for throughput

#### 4. Resource Optimization
```csharp
// Configure export batch settings
services.Configure<BatchExportActivityProcessorOptions>(options =>
{
    options.MaxExportBatchSize = 512;
    options.ScheduledDelayMilliseconds = 1000;
    options.ExporterTimeoutMilliseconds = 30000;
    options.MaxQueueSize = 2048;
});
```

## Consequences

### Positive Outcomes

1. **Enhanced Observability**
   - End-to-end distributed tracing across all components
   - Automatic correlation of logs, traces, and metrics
   - Deep visibility into system performance and behavior

2. **Improved Debugging & Troubleshooting**
   - Faster root cause analysis with complete request traces
   - Rich contextual information for error scenarios
   - Performance bottleneck identification

3. **Proactive Monitoring**
   - Real-time alerts on system health and performance
   - Custom business metrics and SLA monitoring
   - Capacity planning insights

4. **Developer Productivity**
   - Consistent telemetry patterns across all projects
   - Built-in correlation IDs for log aggregation
   - Automated performance regression detection

5. **Production Readiness**
   - Enterprise-grade monitoring and alerting
   - Compliance with observability best practices
   - Integration with Azure ecosystem

### Potential Challenges

1. **Performance Overhead**
   - **Mitigation**: Careful sampling configuration, async operations, batch export
   - **Monitoring**: Track telemetry system resource usage

2. **Cost Implications**
   - **Mitigation**: Implement sampling, data retention policies
   - **Monitoring**: Application Insights cost monitoring and alerts

3. **Complexity**
   - **Mitigation**: Comprehensive documentation, shared libraries, training
   - **Standard Patterns**: Consistent implementation across projects

4. **Data Volume**
   - **Mitigation**: Sampling strategies, attribute filtering, retention policies
   - **Optimization**: Regular review of telemetry data utility

### Migration Path

#### Phase 1: Foundation (Week 1-2)
- Add OpenTelemetry packages to all projects
- Configure basic automatic instrumentation
- Set up Application Insights workspace and dashboards

#### Phase 2: API Layer (Week 2-3)
- Implement API endpoint instrumentation
- Add business operation spans
- Configure structured logging

#### Phase 3: CLI & Client (Week 3-4)
- Instrument CLI commands and HTTP client calls
- Add correlation between CLI operations and API calls
- Test end-to-end tracing scenarios

#### Phase 4: Storage & Advanced Features (Week 4-5)
- Implement storage layer instrumentation
- Add custom business metrics
- Configure alerts and monitoring dashboards

#### Phase 5: Testing & Documentation (Week 5-6)
- Comprehensive testing of telemetry functionality
- Performance testing with telemetry enabled
- Documentation and team training

## Alternatives Considered

### 1. Application Insights SDK Only
**Pros**: Simpler integration, fewer dependencies
**Cons**: Vendor lock-in, limited standardization, less flexibility
**Verdict**: Rejected - OpenTelemetry provides better future-proofing

### 2. Custom Logging Solution
**Pros**: Full control, minimal dependencies
**Cons**: Significant development effort, maintenance overhead, no standard tooling
**Verdict**: Rejected - Reinventing the wheel with inferior results

### 3. Third-Party APM Solutions
**Pros**: Rich feature sets, proven solutions
**Cons**: Additional costs, vendor lock-in, complex integration
**Verdict**: Rejected - OpenTelemetry with Application Insights provides optimal balance

### 4. Metrics-Only Approach
**Pros**: Lower complexity and overhead
**Cons**: Missing distributed tracing, limited debugging capabilities
**Verdict**: Rejected - Insufficient for complex multi-service architecture

## References

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Azure Monitor OpenTelemetry Integration](https://docs.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-overview)
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/reference/specification/trace/semantic_conventions/)
- [Application Insights Telemetry Data Model](https://docs.microsoft.com/en-us/azure/azure-monitor/app/data-model)
- [.NET Observability Best Practices](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/observability-best-practices)

---

**ADR Author**: Claude Code Assistant  
**Review Date**: 2025-01-09  
**Next Review**: 2025-04-09 (Quarterly)