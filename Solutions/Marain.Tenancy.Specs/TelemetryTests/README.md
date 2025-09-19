# Telemetry Testing Framework

This directory contains the telemetry testing framework and example tests for validating OpenTelemetry instrumentation in the Marain.Tenancy solution.

## Overview

The telemetry testing framework provides utilities to:
- Capture activities (spans) and metrics during test execution
- Validate telemetry data against expected values
- Test correlation and end-to-end tracing scenarios
- Measure telemetry overhead and performance impact

## Key Components

### TelemetryTestHelpers
Static helper class providing utilities for telemetry testing:
- `CreateTestActivityListener()` - Captures activities for validation
- `CreateTestMeterListener()` - Captures metrics for validation  
- `ValidateActivity()` - Validates activity properties and tags
- `ValidateMetric()` - Validates metric values and tags
- `WaitForActivitiesAsync()` - Waits for expected activities to be captured
- `WaitForMetricAsync()` - Waits for expected metrics to be recorded

### TelemetryTestScope
Disposable test scope that manages listeners and provides convenient methods:
- Automatically sets up activity and meter listeners
- Provides captured activities and measurements collections
- Includes validation helper methods
- Properly disposes resources after test completion

### MeasurementCapture
Data class representing a captured metric measurement:
- `Value` - The measured value
- `Tags` - Dictionary of measurement tags
- `Timestamp` - When the measurement was captured

## Usage Examples

### Basic Activity Validation

```csharp
[Test]
public async Task MyOperation_ShouldCreateActivity()
{
    using TelemetryTestScope scope = TelemetryTestHelpers.CreateTelemetryTestScope();
    
    // Act
    await systemUnderTest.PerformOperationAsync("tenant-123");
    
    // Assert
    await scope.WaitForActivitiesAsync(1);
    
    Activity activity = scope.ValidateActivity(
        "my-operation",
        expectedTags: new Dictionary<string, object?>
        {
            ["tenant.id"] = "tenant-123",
            ["operation.type"] = "get"
        },
        expectedStatus: ActivityStatusCode.Ok);
        
    Assert.That(activity, Is.Not.Null);
}
```

### Metric Validation

```csharp
[Test]
public async Task MyOperation_ShouldRecordMetrics()
{
    using TelemetryTestScope scope = TelemetryTestHelpers.CreateTelemetryTestScope();
    
    // Act
    await systemUnderTest.PerformOperationAsync();
    
    // Assert
    await scope.WaitForMetricAsync("my.operation.total");
    
    List<MeasurementCapture> measurements = scope.ValidateMetric(
        "my.operation.total",
        expectedValue: 1L,
        expectedTags: new Dictionary<string, object?>
        {
            ["status"] = "success"
        });
        
    Assert.That(measurements, Is.Not.Empty);
}
```

### Error Scenario Testing

```csharp
[Test]
public async Task MyOperation_Error_ShouldRecordErrorMetrics()
{
    using TelemetryTestScope scope = TelemetryTestHelpers.CreateTelemetryTestScope();
    
    // Arrange - Set up error condition
    mockDependency.Setup(x => x.Method()).Throws<InvalidOperationException>();
    
    // Act & Assert
    Assert.ThrowsAsync<InvalidOperationException>(
        () => systemUnderTest.PerformOperationAsync());
    
    // Validate error telemetry
    scope.ValidateActivity(
        "my-operation",
        expectedStatus: ActivityStatusCode.Error);
        
    scope.ValidateMetric(
        "my.operation.errors.total",
        expectedTags: new Dictionary<string, object?>
        {
            ["error.type"] = "InvalidOperationException"
        });
}
```

## Test Structure

### Unit Tests
- **StorageTelemetryTests.cs** - Tests for storage layer telemetry
- **BusinessLogicTelemetryTests.cs** - Tests for business logic telemetry

### Integration Tests  
- **EndToEndTelemetryTests.cs** - End-to-end tracing and correlation tests

## Running the Tests

### Prerequisites
- .NET 8.0 SDK
- NUnit test framework
- NSubstitute for mocking

### Command Line
```bash
# Run all telemetry tests
dotnet test --filter "Category=Telemetry"

# Run specific test class
dotnet test --filter "ClassName=StorageTelemetryTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed" --filter "Category=Telemetry"
```

### Visual Studio
- Open Test Explorer
- Filter tests by "Telemetry" category or namespace
- Run individual tests or test classes

## Test Categories

Tests are organized into the following categories:

### Unit Tests
- Test individual components in isolation
- Use mocked dependencies 
- Focus on specific telemetry patterns
- Fast execution (< 100ms per test)

### Integration Tests
- Test multiple components together
- May use real dependencies or test containers
- Validate end-to-end scenarios
- Moderate execution time (< 1s per test)

### Performance Tests
- Measure telemetry overhead
- Validate performance impact is minimal
- Test high-volume scenarios
- Focus on scalability concerns

## Best Practices

### Test Design
1. **Isolation** - Each test should be independent
2. **Cleanup** - Always dispose test scopes
3. **Timeouts** - Use appropriate timeouts for async operations
4. **Clear Names** - Test names should describe the scenario and expected outcome

### Assertions
1. **Specific** - Validate specific telemetry values, not just presence
2. **Complete** - Test both success and error scenarios
3. **Realistic** - Use realistic test data and scenarios
4. **Performance** - Include performance validation where appropriate

### Mock Usage
1. **Minimal** - Mock only external dependencies
2. **Realistic** - Mock responses should be realistic
3. **Error Cases** - Include error scenario mocks
4. **Consistent** - Use consistent mocking patterns

## Common Patterns

### Activity Testing Pattern
```csharp
// Arrange
using TelemetryTestScope scope = TelemetryTestHelpers.CreateTelemetryTestScope();

// Act
await PerformOperation();

// Assert
await scope.WaitForActivitiesAsync(1);
Activity activity = scope.ValidateActivity("operation-name", expectedTags, expectedStatus);
```

### Metric Testing Pattern
```csharp
// Arrange
using TelemetryTestScope scope = TelemetryTestHelpers.CreateTelemetryTestScope();

// Act
await PerformOperation();

// Assert
await scope.WaitForMetricAsync("metric-name");
List<MeasurementCapture> measurements = scope.ValidateMetric("metric-name", expectedValue, expectedTags);
```

### Error Testing Pattern
```csharp
// Arrange
using TelemetryTestScope scope = TelemetryTestHelpers.CreateTelemetryTestScope();
SetupErrorCondition();

// Act & Assert
Assert.ThrowsAsync<ExpectedException>(() => PerformOperation());

// Validate error telemetry
scope.ValidateActivity("operation-name", expectedStatus: ActivityStatusCode.Error);
scope.ValidateMetric("error-metric", expectedTags: errorTags);
```

## Troubleshooting

### Common Issues

1. **Activities not captured**
   - Verify ActivitySource names match constants
   - Check that ActivityListener is properly configured
   - Ensure test waits for async operations to complete

2. **Metrics not recorded**
   - Verify Meter names match constants
   - Check MeterListener configuration
   - Ensure sufficient wait time for metric recording

3. **Flaky tests**
   - Increase timeout values for slower CI environments
   - Add proper async/await patterns
   - Ensure proper test isolation

4. **Performance issues**
   - Check for resource leaks (undisposed scopes)
   - Verify test parallelization settings
   - Consider reducing test data volume

### Debugging Tips

1. **Enable verbose logging** in test configuration
2. **Add diagnostic output** to understand test execution flow
3. **Use debugger breakpoints** to inspect telemetry data
4. **Check Activity.Current** context during test execution
5. **Validate test environment setup** matches production configuration

## Contributing

When adding new telemetry tests:

1. Follow the established patterns and naming conventions
2. Include both positive and negative test cases
3. Add appropriate test categories and documentation
4. Ensure tests are deterministic and isolated
5. Validate performance impact is minimal
6. Update this documentation as needed

## References

- [OpenTelemetry .NET Testing](https://opentelemetry.io/docs/instrumentation/net/getting-started/)
- [Activity and ActivitySource Documentation](https://docs.microsoft.com/dotnet/api/system.diagnostics.activity)
- [System.Diagnostics.Metrics Documentation](https://docs.microsoft.com/dotnet/api/system.diagnostics.metrics)
- [NUnit Testing Framework](https://docs.nunit.org/)
- [NSubstitute Mocking Framework](https://nsubstitute.github.io/)