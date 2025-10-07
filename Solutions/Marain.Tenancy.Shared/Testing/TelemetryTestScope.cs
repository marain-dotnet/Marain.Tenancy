// <copyright file="TelemetryTestScope.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Shared.Testing;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marain.Tenancy.Shared.Telemetry;

/// <summary>
/// Disposable test scope that manages activity and meter listeners for telemetry testing.
/// </summary>
public sealed class TelemetryTestScope : IDisposable
{
    private readonly ActivityListener activityListener;
    private readonly MeterListener meterListener;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryTestScope"/> class.
    /// </summary>
    internal TelemetryTestScope()
    {
        this.CapturedActivities = new List<Activity>();
        this.CapturedMeasurements = new Dictionary<string, List<MeasurementCapture>>();

        this.activityListener = TelemetryTestHelpers.CreateTestActivityListener(this.CapturedActivities);
        this.meterListener = TelemetryTestHelpers.CreateTestMeterListener(this.CapturedMeasurements);

        ActivitySource.AddActivityListener(this.activityListener);
        this.meterListener.Start();
    }

    /// <summary>
    /// Gets the list of captured activities.
    /// </summary>
    public List<Activity> CapturedActivities { get; }

    /// <summary>
    /// Gets the dictionary of captured measurements.
    /// </summary>
    public Dictionary<string, List<MeasurementCapture>> CapturedMeasurements { get; }

    /// <summary>
    /// Validates that a specific activity was created with expected properties.
    /// </summary>
    /// <param name="activityName">The expected activity name.</param>
    /// <param name="expectedTags">Dictionary of expected tag key-value pairs.</param>
    /// <param name="expectedStatus">The expected activity status.</param>
    /// <returns>The matching activity if found.</returns>
    public Activity ValidateActivity(
        string activityName,
        Dictionary<string, object?>? expectedTags = null,
        ActivityStatusCode? expectedStatus = null)
    {
        return TelemetryTestHelpers.ValidateActivity(this.CapturedActivities, activityName, expectedTags, expectedStatus);
    }

    /// <summary>
    /// Validates that a specific metric was recorded with expected properties.
    /// </summary>
    /// <param name="metricName">The expected metric name.</param>
    /// <param name="expectedValue">The expected metric value (optional).</param>
    /// <param name="expectedTags">Dictionary of expected tag key-value pairs (optional).</param>
    /// <param name="minimumCount">Minimum number of measurements expected (default: 1).</param>
    /// <returns>The list of matching measurements.</returns>
    public List<MeasurementCapture> ValidateMetric(
        string metricName,
        object? expectedValue = null,
        Dictionary<string, object?>? expectedTags = null,
        int minimumCount = 1)
    {
        return TelemetryTestHelpers.ValidateMetric(this.CapturedMeasurements, metricName, expectedValue, expectedTags, minimumCount);
    }

    /// <summary>
    /// Waits for a specific number of activities to be captured.
    /// </summary>
    /// <param name="expectedCount">The expected number of activities.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <returns>A task that completes when the expected count is reached.</returns>
    public Task WaitForActivitiesAsync(int expectedCount, TimeSpan? timeout = null)
    {
        return TelemetryTestHelpers.WaitForActivitiesAsync(this.CapturedActivities, expectedCount, timeout);
    }

    /// <summary>
    /// Waits for a specific metric to be recorded.
    /// </summary>
    /// <param name="metricName">The expected metric name.</param>
    /// <param name="minimumCount">Minimum number of measurements to wait for.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <returns>A task that completes when the metric is recorded.</returns>
    public Task WaitForMetricAsync(string metricName, int minimumCount = 1, TimeSpan? timeout = null)
    {
        return TelemetryTestHelpers.WaitForMetricAsync(this.CapturedMeasurements, metricName, minimumCount, timeout);
    }

    /// <summary>
    /// Clears all captured activities and measurements.
    /// </summary>
    public void Clear()
    {
        this.CapturedActivities.Clear();
        this.CapturedMeasurements.Clear();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!this.disposed)
        {
            this.activityListener?.Dispose();
            this.meterListener?.Dispose();
            this.disposed = true;
        }
    }
}