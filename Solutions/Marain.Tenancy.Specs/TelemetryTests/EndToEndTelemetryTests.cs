// <copyright file="EndToEndTelemetryTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.TelemetryTests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using Marain.Tenancy.Shared.Telemetry;
using Marain.Tenancy.Shared.Testing;
using NUnit.Framework;

/// <summary>
/// End-to-end integration tests for telemetry validation.
/// </summary>
[TestFixture]
public class EndToEndTelemetryTests
{
    private TelemetryTestScope? telemetryScope;

    /// <summary>
    /// Sets up the test environment before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this.telemetryScope = TelemetryTestHelpers.CreateTelemetryTestScope();
    }

    /// <summary>
    /// Cleans up after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        this.telemetryScope?.Dispose();
    }

    /// <summary>
    /// Tests that telemetry correlation works across service boundaries.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task TelemetryCorrelation_ShouldMaintainTraceContext()
    {
        // Arrange - Create a parent activity to simulate an incoming request
        using ActivitySource testSource = new("Marain.Tenancy.Test");
        using Activity? parentActivity = testSource.StartActivity("incoming-request");

        string correlationId = Activity.Current?.Id ?? "test-correlation";
        parentActivity?.SetTag("correlation.id", correlationId);

        // Act - Simulate multiple operations that would happen in a real scenario
        await SimulateApiRequest();
        await SimulateBusinessLogicCall();
        await SimulateStorageOperation();

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(3, TimeSpan.FromSeconds(5));

        // Assert - Verify all activities are correlated
        List<Activity> capturedActivities = this.telemetryScope.CapturedActivities;
        Assert.That(capturedActivities.Count, Is.GreaterThanOrEqualTo(3));

        // Verify activities have trace context correlation
        string? traceId = capturedActivities.FirstOrDefault()?.TraceId.ToString();
        Assert.That(traceId, Is.Not.Null.And.Not.Empty);

        // All activities should share the same trace ID
        foreach (Activity activity in capturedActivities)
        {
            Assert.That(
                activity.TraceId.ToString(),
                Is.EqualTo(traceId),
                $"Activity '{activity.DisplayName}' has different trace ID");
        }
    }

    /// <summary>
    /// Tests that metrics aggregation works correctly across operations.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task MetricsAggregation_ShouldAccumulateCorrectly()
    {
        // Arrange - Simulate multiple operations of the same type
        int operationCount = 5;

        // Act - Perform multiple operations
        for (int i = 0; i < operationCount; i++)
        {
            await SimulateBusinessLogicCall();
            await Task.Delay(10); // Small delay to ensure operations are distinct
        }

        // Wait for metrics to be captured
        await this.telemetryScope!.WaitForMetricAsync("tenant.business.operations.total", operationCount, TimeSpan.FromSeconds(5));

        // Assert - Verify metrics accumulation
        List<MeasurementCapture> operationMetrics = this.telemetryScope.ValidateMetric(
            "tenant.business.operations.total",
            minimumCount: operationCount);

        // Calculate total operations
        long totalOperations = operationMetrics.Sum(m => Convert.ToInt64(m.Value));
        Assert.That(totalOperations, Is.EqualTo(operationCount));

        // Verify all operations have correct tags
        foreach (MeasurementCapture measurement in operationMetrics)
        {
            Assert.That(measurement.Tags, Contains.Key(TelemetryConstants.AttributeKeys.OperationType));
            Assert.That(measurement.Tags, Contains.Key("status"));
        }
    }

    /// <summary>
    /// Tests that error scenarios generate comprehensive telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ErrorScenarios_ShouldGenerateComprehensiveTelemetry()
    {
        // Act - Simulate error scenarios
        await SimulateOperationWithError("ValidationError");
        await SimulateOperationWithError("TimeoutError");

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(2, TimeSpan.FromSeconds(5));

        // Assert - Verify error activities
        var errorActivities = this.telemetryScope.CapturedActivities
            .Where(a => a.Status == ActivityStatusCode.Error)
            .ToList();

        Assert.That(errorActivities.Count, Is.EqualTo(2));

        // Verify error metrics
        this.telemetryScope.ValidateMetric(
            "tenant.business.errors.total",
            minimumCount: 2);

        // Verify operation metrics with error status
        List<MeasurementCapture> errorOperationMetrics = this.telemetryScope.ValidateMetric(
            "tenant.business.operations.total",
            expectedTags: new Dictionary<string, object?>
            {
                ["status"] = "error",
            },
            minimumCount: 2);

        Assert.That(errorOperationMetrics.Count, Is.EqualTo(2));
    }

    /// <summary>
    /// Tests that performance metrics are recorded with appropriate precision.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task PerformanceMetrics_ShouldRecordWithAppropritePrecision()
    {
        // Act - Simulate operations with varying durations
        await SimulateSlowOperation(100); // 100ms operation
        await SimulateFastOperation(10);  // 10ms operation

        // Wait for metrics to be captured
        await this.telemetryScope!.WaitForMetricAsync("tenant.business.operation.duration", 2, TimeSpan.FromSeconds(5));

        // Assert - Verify duration metrics
        List<MeasurementCapture> durationMetrics = this.telemetryScope.ValidateMetric(
            "tenant.business.operation.duration",
            minimumCount: 2);

        // Verify duration values are reasonable
        foreach (MeasurementCapture measurement in durationMetrics)
        {
            double duration = Convert.ToDouble(measurement.Value);
            Assert.That(duration, Is.GreaterThan(0));
            Assert.That(duration, Is.LessThan(1000)); // Should be less than 1 second for test operations
        }

        // Verify we have both slow and fast operations recorded
        var durations = durationMetrics.Select(m => Convert.ToDouble(m.Value)).Order().ToList();
        Assert.That(durations[0], Is.LessThan(durations[1]), "Should have operations with different durations");
    }

    /// <summary>
    /// Tests that telemetry overhead is minimal.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task TelemetryOverhead_ShouldBeMinimal()
    {
        // Act - Perform many operations to test overhead
        Stopwatch watch = new();
        watch.Start();

        const int operationCount = 100;
        foreach (int i in Enumerable.Range(0, operationCount))
        {
            await SimulateFastOperation(1);
        }

        watch.Stop();

        // Assert - Verify telemetry overhead is reasonable
        double averageOperationTime = watch.ElapsedMilliseconds / operationCount;

        // Each operation should take much less than 20ms on average (including telemetry overhead)
        Assert.That(
            averageOperationTime,
            Is.LessThan(20),
            $"Average operation time {averageOperationTime:F2}ms suggests high telemetry overhead");

        // Verify all operations were captured
        await this.telemetryScope!.WaitForMetricAsync("tenant.business.operations.total", operationCount, TimeSpan.FromSeconds(10));
    }

    // Helper methods to simulate different types of operations
    private static async Task SimulateApiRequest()
    {
        using ActivitySource apiSource = new(TelemetryConstants.ApiActivitySource);
        using Activity? activity = apiSource.StartActivity("api.tenant.get");
        activity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, "test-tenant-123");
        activity?.SetTag(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get);

        await Task.Delay(10); // Simulate API processing time
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static async Task SimulateBusinessLogicCall()
    {
        using ActivitySource businessSource = new(TelemetryConstants.BusinessActivitySource);
        using Activity? activity = businessSource.StartActivity("business.tenant.get");
        activity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, "test-tenant-123");
        activity?.SetTag(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get);

        // Simulate business logic metrics
        using Meter meter = new(TelemetryConstants.TenancyMeter);
        Counter<long> operationsCounter = meter.CreateCounter<long>("tenant.business.operations.total");
        Histogram<double> durationHistogram = meter.CreateHistogram<double>("tenant.business.operation.duration");

        var stopwatch = Stopwatch.StartNew();
        await Task.Delay(15); // Simulate business processing time
        durationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);

        operationsCounter.Add(
            1,
            new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
            new KeyValuePair<string, object?>("status", "success"));

        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static async Task SimulateStorageOperation()
    {
        using ActivitySource storageSource = new(TelemetryConstants.StorageActivitySource);
        using Activity? activity = storageSource.StartActivity("storage.tenant.get");
        activity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, "test-tenant-123");
        activity?.SetTag(TelemetryConstants.AttributeKeys.StorageOperation, TelemetryConstants.OperationTypes.Get);

        await Task.Delay(25); // Simulate storage operation time
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static async Task SimulateOperationWithError(string errorType)
    {
        using ActivitySource businessSource = new(TelemetryConstants.BusinessActivitySource);
        using Activity? activity = businessSource.StartActivity("business.tenant.operation-with-error");
        activity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, "error-tenant-456");
        activity?.SetTag(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get);

        // Simulate error metrics
        using Meter meter = new(TelemetryConstants.TenancyMeter);
        Counter<long> errorCounter = meter.CreateCounter<long>("tenant.business.errors.total");
        Counter<long> operationsCounter = meter.CreateCounter<long>("tenant.business.operations.total");

        await Task.Delay(20); // Simulate processing time before error

        errorCounter.Add(
            1,
            new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
            new KeyValuePair<string, object?>("error.type", errorType));

        operationsCounter.Add(
            1,
            new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Get),
            new KeyValuePair<string, object?>("status", "error"));

        activity?.SetStatus(ActivityStatusCode.Error, $"Simulated {errorType}");
    }

    private static async Task SimulateSlowOperation(int delayMs)
    {
        using ActivitySource businessSource = new(TelemetryConstants.BusinessActivitySource);
        using Activity? activity = businessSource.StartActivity("business.slow-operation");

        using Meter meter = new(TelemetryConstants.TenancyMeter);
        Histogram<double> durationHistogram = meter.CreateHistogram<double>("tenant.business.operation.duration");
        Counter<long> operationsCounter = meter.CreateCounter<long>("tenant.business.operations.total");

        var stopwatch = Stopwatch.StartNew();
        await Task.Delay(delayMs);
        durationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);

        operationsCounter.Add(
            1,
            new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, "slow"),
            new KeyValuePair<string, object?>("status", "success"));

        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static async Task SimulateFastOperation(int delayMs)
    {
        using ActivitySource businessSource = new(TelemetryConstants.BusinessActivitySource);
        using Activity? activity = businessSource.StartActivity("business.fast-operation");

        using Meter meter = new(TelemetryConstants.TenancyMeter);
        Histogram<double> durationHistogram = meter.CreateHistogram<double>("tenant.business.operation.duration");
        Counter<long> operationsCounter = meter.CreateCounter<long>("tenant.business.operations.total");

        var stopwatch = Stopwatch.StartNew();
        await Task.Delay(delayMs);
        durationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds);

        operationsCounter.Add(
            1,
            new KeyValuePair<string, object?>(TelemetryConstants.AttributeKeys.OperationType, "fast"),
            new KeyValuePair<string, object?>("status", "success"));

        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}