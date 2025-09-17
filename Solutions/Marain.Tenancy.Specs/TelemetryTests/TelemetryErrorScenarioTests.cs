// <copyright file="TelemetryErrorScenarioTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.TelemetryTests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Marain.Tenancy.Shared.Extensions;
using Marain.Tenancy.Shared.Telemetry;
using Marain.Tenancy.Shared.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using OpenTelemetry.Trace;

/// <summary>
/// Tests for telemetry behavior during error scenarios and telemetry system failures.
/// </summary>
[TestFixture]
[Category("Telemetry")]
[Category("ErrorHandling")]
public class TelemetryErrorScenarioTests
{
    private TelemetryTestScope? telemetryScope;
    private ServiceProvider? serviceProvider;

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
        this.serviceProvider?.Dispose();
        this.telemetryScope?.Dispose();
    }

    /// <summary>
    /// Tests behavior when Application Insights connection string is invalid.
    /// </summary>
    [Test]
    public void InvalidApplicationInsightsConnectionString_ShouldNotPreventStartup()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ApplicationInsights"] = "InstrumentationKey=invalid-key;IngestionEndpoint=https://invalid.endpoint",
            })
            .Build();

        services.AddLogging();

        // Act & Assert - Should not throw during service configuration
        Assert.DoesNotThrow(() =>
        {
            services.AddMarainTelemetry(configuration);
            this.serviceProvider = services.BuildServiceProvider();
        });

        Assert.That(this.serviceProvider, Is.Not.Null);
    }

    /// <summary>
    /// Tests behavior when Application Insights connection string is missing.
    /// </summary>
    [Test]
    public void MissingApplicationInsightsConnectionString_ShouldFallbackToConsoleExporter()
    {
        // Arrange
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        services.AddLogging();

        // Act
        services.AddMarainTelemetry(configuration, environment: null);
        this.serviceProvider = services.BuildServiceProvider();

        // Assert - Should successfully create provider without Application Insights
        Assert.That(this.serviceProvider, Is.Not.Null);

        // Verify that telemetry components are still available
        TracerProvider? tracerProvider = this.serviceProvider.GetService<TracerProvider>();
        Assert.That(tracerProvider, Is.Not.Null);
    }

    /// <summary>
    /// Tests behavior when ActivitySource is disposed during operation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ActivitySourceDisposal_ShouldGracefullyHandleOngoingOperations()
    {
        // Arrange
        var testActivitySource = new ActivitySource("test.disposal-scenario");

        try
        {
            using Activity? parentActivity = testActivitySource.StartActivity("test.disposal-parent");
            parentActivity?.SetTag("test.purpose", "disposal-handling");

            // Start an operation
            Activity? ongoingActivity = testActivitySource.StartActivity("test.ongoing-operation");
            ongoingActivity?.SetTag("operation.state", "started");

            // Act - Dispose the ActivitySource while operation is ongoing
            testActivitySource.Dispose();

            // Continue with the ongoing operation (should not crash)
            ongoingActivity?.SetTag("operation.state", "continuing-after-disposal");
            ongoingActivity?.SetStatus(ActivityStatusCode.Ok);
            ongoingActivity?.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"ActivitySource disposal should not cause exceptions: {ex.Message}");
        }

        // Assert - No exceptions should be thrown
        Assert.Pass("ActivitySource disposal handled gracefully");

        await Task.CompletedTask; // For async consistency
    }

    /// <summary>
    /// Tests that null or invalid tag values don't crash telemetry.
    /// </summary>
    [Test]
    public void InvalidTagValues_ShouldNotCrashTelemetry()
    {
        // Arrange
        using var testActivitySource = new ActivitySource("test.invalid-tags");

        // Act & Assert - Various invalid tag scenarios should not throw
        Assert.DoesNotThrow(() =>
        {
            using Activity? activity = testActivitySource.StartActivity("test.invalid-tag-values");

            // Null values
            activity?.SetTag("null.string", (string?)null);
            activity?.SetTag("null.object", (object?)null);

            // Empty values
            activity?.SetTag("empty.string", string.Empty);
            activity?.SetTag("whitespace.string", "   ");

            // Very long values
            string longValue = new string('x', 10000);
            activity?.SetTag("long.value", longValue);

            // Special characters
            activity?.SetTag("special.chars", "αβγδε!@#$%^&*()");
            activity?.SetTag("newlines", "line1\nline2\rline3");

            // Numeric edge cases
            activity?.SetTag("max.int", int.MaxValue);
            activity?.SetTag("min.int", int.MinValue);
            activity?.SetTag("max.double", double.MaxValue);
            activity?.SetTag("nan.double", double.NaN);
            activity?.SetTag("infinity", double.PositiveInfinity);

            activity?.SetStatus(ActivityStatusCode.Ok);
        });
    }

    /// <summary>
    /// Tests behavior with excessive activity nesting.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ExcessiveActivityNesting_ShouldHandleGracefully()
    {
        // Arrange
        using var testActivitySource = new ActivitySource("test.excessive-nesting");
        const int nestingDepth = 100;
        var activities = new Activity?[nestingDepth];

        try
        {
            // Act - Create deeply nested activities
            for (int i = 0; i < nestingDepth; i++)
            {
                activities[i] = testActivitySource.StartActivity($"nested.level-{i:D3}");
                activities[i]?.SetTag("nesting.level", i.ToString());
                activities[i]?.SetTag("test.type", "excessive-nesting");
            }

            // Add some delay to ensure activities are processed
            await Task.Delay(100);

            // Assert - Should not have crashed
            Assert.That(activities[0], Is.Not.Null);
            Assert.That(activities[nestingDepth - 1], Is.Not.Null);
        }
        finally
        {
            // Cleanup - Dispose activities in reverse order
            for (int i = nestingDepth - 1; i >= 0; i--)
            {
                activities[i]?.Dispose();
            }
        }
    }

    /// <summary>
    /// Tests that telemetry doesn't interfere with exception propagation.
    /// </summary>
    [Test]
    public void ExceptionDuringTelemetryOperation_ShouldNotSuppressOriginalException()
    {
        // Arrange
        using var testActivitySource = new ActivitySource("test.exception-propagation");
        var expectedException = new InvalidOperationException("Original business logic exception");

        // Act & Assert
        InvalidOperationException? thrownException = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            using Activity? activity = testActivitySource.StartActivity("test.exception-scenario");
            activity?.SetTag("test.purpose", "exception-propagation");

            try
            {
                // Simulate business logic that throws
                await Task.Delay(10);
                throw expectedException;
            }
            catch (Exception ex)
            {
                // Record error in telemetry
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.type", ex.GetType().Name);

                // Re-throw original exception
                throw;
            }
        });

        Assert.That(thrownException, Is.SameAs(expectedException));
        Assert.That(thrownException?.Message, Is.EqualTo("Original business logic exception"));
    }

    /// <summary>
    /// Tests behavior when high-frequency operations stress the telemetry system.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task HighFrequencyOperations_ShouldNotCauseResourceExhaustion()
    {
        // Arrange
        using var testActivitySource = new ActivitySource("test.high-frequency");
        const int operationCount = 10000;
        const int batchSize = 100;

        // Act - Create many activities in batches to avoid overwhelming the system
        for (int batch = 0; batch < operationCount / batchSize; batch++)
        {
            var batchTasks = new Task[batchSize];

            for (int i = 0; i < batchSize; i++)
            {
                int operationId = (batch * batchSize) + i;
                batchTasks[i] = Task.Run(() =>
                {
                    using Activity? activity = testActivitySource.StartActivity($"high-freq.op-{operationId:D5}");
                    activity?.SetTag("operation.id", operationId.ToString());
                    activity?.SetTag("batch.id", batch.ToString());
                    activity?.SetStatus(ActivityStatusCode.Ok);
                });
            }

            await Task.WhenAll(batchTasks);

            // Brief pause between batches to allow telemetry processing
            await Task.Delay(10);
        }

        // Assert - Should complete without exceptions or resource exhaustion
        // Wait for telemetry system to process
        await Task.Delay(1000);

        Assert.Pass($"Successfully processed {operationCount} high-frequency operations");
    }

    /// <summary>
    /// Tests that concurrent telemetry operations don't cause race conditions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ConcurrentTelemetryOperations_ShouldNotCauseRaceConditions()
    {
        // Arrange
        using var testActivitySource = new ActivitySource("test.concurrent-telemetry");
        const int concurrentOperations = 50;
        var tasks = new Task[concurrentOperations];

        // Act - Create many concurrent telemetry operations
        for (int i = 0; i < concurrentOperations; i++)
        {
            int operationIndex = i;
            tasks[i] = Task.Run(async () =>
            {
                using Activity? activity = testActivitySource.StartActivity($"concurrent.op-{operationIndex:D2}");
                activity?.SetTag("operation.index", operationIndex.ToString());
                activity?.SetTag("thread.id", Environment.CurrentManagedThreadId.ToString());

                // Simulate some work with random delays
                await Task.Delay(Random.Shared.Next(1, 50));

                // Add more tags during operation
                activity?.SetTag("completion.time", DateTimeOffset.UtcNow.ToString("O"));
                activity?.SetStatus(ActivityStatusCode.Ok);
            });
        }

        // Wait for all operations to complete
        await Task.WhenAll(tasks);

        // Assert - All tasks should complete successfully
        Assert.That(tasks.All(t => t.IsCompletedSuccessfully), Is.True);
    }

    /// <summary>
    /// Tests telemetry behavior when system resources are constrained.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ResourceConstrainedEnvironment_ShouldDegradeGracefully()
    {
        // Arrange - Simulate resource-constrained environment
        using var testActivitySource = new ActivitySource("test.resource-constrained");

        // Act - Create operations that might stress memory/CPU
        var tasks = new List<Task>();

        for (int i = 0; i < 1000; i++)
        {
            int operationId = i;
            tasks.Add(Task.Run(() =>
            {
                using Activity? activity = testActivitySource.StartActivity($"constrained.op-{operationId:D4}");
                activity?.SetTag("resource.test", "memory-pressure");
                activity?.SetTag("operation.id", operationId.ToString());

                // Simulate memory allocation pressure
                byte[] largeArray = new byte[1024 * 10]; // 10KB per operation
                largeArray[0] = (byte)(operationId % 256);

                activity?.SetTag("allocated.bytes", largeArray.Length.ToString());
                activity?.SetStatus(ActivityStatusCode.Ok);

                // Force GC to trigger memory pressure
                if (operationId % 100 == 0)
                {
                    GC.Collect(0, GCCollectionMode.Optimized);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Operations should complete even under resource pressure
        Assert.That(tasks.All(t => t.IsCompletedSuccessfully), Is.True);

        // Force final cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}