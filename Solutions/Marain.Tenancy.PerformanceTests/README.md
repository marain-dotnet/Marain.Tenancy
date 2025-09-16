# Marain.Tenancy Performance Tests

This project contains performance benchmarks and load tests for the Marain.Tenancy solution, specifically focused on measuring telemetry overhead and validating performance under various load conditions.

## Overview

The performance testing framework uses BenchmarkDotNet to provide accurate, reliable performance measurements with statistical analysis. The tests compare baseline operations without telemetry against the same operations with full OpenTelemetry instrumentation enabled.

## Test Categories

### Baseline Benchmarks (`TenantOperationsBenchmarks`)
- **Purpose**: Establish baseline performance metrics without telemetry overhead
- **Operations**: Get tenant, create child tenant, multiple operations, concurrent operations
- **Key Metrics**: Execution time, memory allocation, throughput

### Telemetry Overhead Benchmarks (`TelemetryOverheadBenchmarks`)
- **Purpose**: Measure the performance impact of OpenTelemetry instrumentation
- **Operations**: Same as baseline but with full telemetry instrumentation
- **Key Metrics**: Overhead percentage, memory impact, instrumentation cost

### Load Testing Benchmarks (`LoadTestBenchmarks`)
- **Purpose**: Validate telemetry performance under high-volume, concurrent scenarios
- **Operations**: Concurrent retrievals, creations, mixed workloads, sustained load
- **Key Metrics**: Throughput under load, resource utilization, export buffer behavior

## Key Performance Requirements

Based on the ADR specifications, telemetry overhead should be:
- **< 5%** performance impact on critical operations
- **Minimal memory overhead** for typical workloads
- **Scalable** to handle 1000+ operations per minute
- **Stable** under sustained load conditions

## Running the Benchmarks

### Prerequisites
- .NET 8.0 SDK
- Release configuration (for accurate measurements)
- Sufficient system resources (recommend 8GB+ RAM)
- No other heavy processes running during benchmarks

### Command Line Execution

```bash
# Build in Release mode (required for accurate benchmarks)
dotnet build -c Release Solutions/Marain.Tenancy.PerformanceTests/Marain.Tenancy.PerformanceTests.csproj

# Run all benchmarks
dotnet run -c Release --project Solutions/Marain.Tenancy.PerformanceTests/Marain.Tenancy.PerformanceTests.csproj

# Run specific benchmark class
dotnet run -c Release --project Solutions/Marain.Tenancy.PerformanceTests/Marain.Tenancy.PerformanceTests.csproj -- --filter "*TenantOperationsBenchmarks*"

# Run with memory diagnoser and export results
dotnet run -c Release --project Solutions/Marain.Tenancy.PerformanceTests/Marain.Tenancy.PerformanceTests.csproj -- --memory --exporters json,html
```

### Benchmark Execution Options

```bash
# Quick run (fewer iterations for faster feedback)
dotnet run -c Release --project Solutions/Marain.Tenancy.PerformanceTests/Marain.Tenancy.PerformanceTests.csproj -- --job short

# Full statistical analysis (recommended for official results)
dotnet run -c Release --project Solutions/Marain.Tenancy.PerformanceTests/Marain.Tenancy.PerformanceTests.csproj -- --job long

# Specific concurrent operation count
dotnet run -c Release --project Solutions/Marain.Tenancy.PerformanceTests/Marain.Tenancy.PerformanceTests.csproj -- --filter "*LoadTestBenchmarks*" --params ConcurrentOperations=100
```

## Interpreting Results

### Key Metrics to Monitor

1. **Mean Execution Time**
   - Baseline: Typical operation without telemetry
   - With Telemetry: Same operation with full instrumentation
   - **Target**: < 5% overhead

2. **Memory Allocation**
   - Gen 0/1/2 collections per operation
   - Allocated memory per operation
   - **Target**: Minimal additional allocations

3. **Throughput (Operations/sec)**
   - Concurrent operation performance
   - Load testing scalability
   - **Target**: Linear scaling with resources

4. **Error Rate**
   - Should remain 0% under normal load
   - Monitor for resource exhaustion

### Performance Thresholds

| Metric | Baseline | With Telemetry | Max Overhead |
|--------|----------|----------------|--------------|
| Tenant Get | ~1ms | ~1.05ms | 5% |
| Tenant Create | ~5ms | ~5.25ms | 5% |
| Memory/Op | ~500B | ~600B | 20% |
| Concurrent Ops (100) | ~10ms | ~11ms | 10% |

## Benchmark Configuration

### BenchmarkDotNet Settings
- **Job**: SimpleJob (single run with multiple iterations)
- **Memory Diagnoser**: Enabled for allocation tracking
- **Statistics**: Mean, StdDev, Min, Max, Median
- **Export Formats**: Console, HTML, JSON

### Environment Requirements
- **Isolation**: Run on dedicated machines or containers
- **Consistency**: Same hardware/software configuration
- **Baseline**: Establish baseline measurements before changes
- **Repeatability**: Run multiple times to verify consistency

## Integration with CI/CD

### Automated Performance Testing
```yaml
# Example Azure DevOps pipeline step
- task: DotNetCoreCLI@2
  displayName: 'Run Performance Benchmarks'
  inputs:
    command: 'run'
    projects: 'Solutions/Marain.Tenancy.PerformanceTests/Marain.Tenancy.PerformanceTests.csproj'
    arguments: '-c Release -- --filter "*Baseline*" --exporters json'

- task: PublishTestResults@2
  displayName: 'Publish Performance Results'
  inputs:
    testResultsFormat: 'VSTest'
    testResultsFiles: '**/BenchmarkDotNet.Artifacts/**/results.xml'
```

### Performance Regression Detection
- Establish baseline measurements for each release
- Compare current results against historical baselines
- Alert on performance regressions > 10%
- Track trends over time

## Troubleshooting

### Common Issues

1. **Inconsistent Results**
   - Ensure Release configuration
   - Close other applications
   - Run multiple times and compare
   - Check for background processes

2. **Memory Issues**
   - Monitor available system memory
   - Reduce concurrent operation counts
   - Check for memory leaks in test setup

3. **High Overhead**
   - Verify telemetry configuration
   - Check sampling rates
   - Review export settings
   - Validate test isolation

4. **Failed Benchmarks**
   - Check mock setup correctness
   - Verify dependency injection configuration
   - Review exception logs
   - Validate test data

### Performance Analysis

1. **Profile Individual Operations**
   ```csharp
   [Benchmark]
   [MemoryDiagnoser]
   public void ProfileSpecificOperation()
   {
       // Isolate specific operation for detailed analysis
   }
   ```

2. **Compare Configurations**
   ```csharp
   [Params("NoTelemetry", "BasicTelemetry", "FullTelemetry")]
   public string Configuration { get; set; }
   ```

3. **Analyze Memory Patterns**
   - Review Gen 0/1/2 collection frequencies
   - Identify allocation hotspots
   - Monitor for memory leaks

## Contributing

When adding new performance tests:

1. **Follow Naming Conventions**: `{Operation}_{Condition}_Benchmark`
2. **Include Documentation**: Clear descriptions of what's being tested
3. **Set Realistic Params**: Use realistic concurrent operation counts
4. **Add Cleanup**: Proper resource disposal in GlobalCleanup
5. **Validate Results**: Ensure tests produce meaningful, consistent results

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
- [.NET Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/core/performance/)
- [OpenTelemetry Performance Guidelines](https://opentelemetry.io/docs/instrumentation/net/getting-started/#performance)
- [Load Testing Guidelines](https://docs.microsoft.com/en-us/azure/architecture/checklist/dev-ops#load-testing)