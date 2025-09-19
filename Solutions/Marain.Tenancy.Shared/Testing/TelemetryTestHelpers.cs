// <copyright file="TelemetryTestHelpers.cs" company="Endjin Limited">
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
/// Helper class for testing telemetry in unit and integration tests.
/// </summary>
public static class TelemetryTestHelpers
{
    /// <summary>
    /// Creates a test activity listener that captures activities for validation.
    /// </summary>
    /// <param name="capturedActivities">List to capture activities.</param>
    /// <returns>An <see cref="ActivityListener"/> configured for testing.</returns>
    public static ActivityListener CreateTestActivityListener(List<Activity> capturedActivities)
    {
        ArgumentNullException.ThrowIfNull(capturedActivities);

        return new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Marain.Tenancy", StringComparison.Ordinal) ||
                                      source.Name.StartsWith("test.", StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => { /* Activity started - no action needed for testing */ },
            ActivityStopped = activity => capturedActivities.Add(activity),
        };
    }

    /// <summary>
    /// Creates a test meter listener that captures metrics for validation.
    /// </summary>
    /// <param name="capturedMeasurements">Dictionary to capture measurements by instrument name.</param>
    /// <returns>A <see cref="MeterListener"/> configured for testing.</returns>
    public static MeterListener CreateTestMeterListener(Dictionary<string, List<MeasurementCapture>> capturedMeasurements)
    {
        ArgumentNullException.ThrowIfNull(capturedMeasurements);

        MeterListener listener = new();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == TelemetryConstants.TenancyMeter)
            {
                listener.EnableMeasurementEvents(instrument, instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
            CaptureMeasurement(capturedMeasurements, instrument.Name, measurement, tags));

        listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
            CaptureMeasurement(capturedMeasurements, instrument.Name, measurement, tags));

        return listener;
    }

    /// <summary>
    /// Validates that a specific activity was created with expected properties.
    /// </summary>
    /// <param name="capturedActivities">The list of captured activities.</param>
    /// <param name="activityName">The expected activity name.</param>
    /// <param name="expectedTags">Dictionary of expected tag key-value pairs.</param>
    /// <param name="expectedStatus">The expected activity status.</param>
    /// <returns>The matching activity if found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the activity is not found or validation fails.</exception>
    public static Activity ValidateActivity(
        IList<Activity> capturedActivities,
        string activityName,
        Dictionary<string, object?>? expectedTags = null,
        ActivityStatusCode? expectedStatus = null)
    {
        ArgumentNullException.ThrowIfNull(capturedActivities);
        ArgumentNullException.ThrowIfNull(activityName);

        Activity? activity = capturedActivities.FirstOrDefault(a => a.DisplayName == activityName);
        if (activity == null)
        {
            string availableActivities = string.Join(", ", capturedActivities.Select(a => a.DisplayName));
            throw new InvalidOperationException($"Activity '{activityName}' not found. Available activities: {availableActivities}");
        }

        if (expectedTags != null)
        {
            foreach (KeyValuePair<string, object?> expectedTag in expectedTags)
            {
                object? actualValue = activity.GetTagItem(expectedTag.Key);
                if (!Equals(actualValue, expectedTag.Value))
                {
                    throw new InvalidOperationException(
                        $"Activity '{activityName}' tag '{expectedTag.Key}' expected '{expectedTag.Value}' but was '{actualValue}'");
                }
            }
        }

        if (expectedStatus.HasValue && activity.Status != expectedStatus.Value)
        {
            throw new InvalidOperationException(
                $"Activity '{activityName}' expected status '{expectedStatus.Value}' but was '{activity.Status}'");
        }

        return activity;
    }

    /// <summary>
    /// Validates that a specific metric was recorded with expected properties.
    /// </summary>
    /// <param name="capturedMeasurements">The captured measurements.</param>
    /// <param name="metricName">The expected metric name.</param>
    /// <param name="expectedValue">The expected metric value (optional).</param>
    /// <param name="expectedTags">Dictionary of expected tag key-value pairs (optional).</param>
    /// <param name="minimumCount">Minimum number of measurements expected (default: 1).</param>
    /// <returns>The list of matching measurements.</returns>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    public static List<MeasurementCapture> ValidateMetric(
        Dictionary<string, List<MeasurementCapture>> capturedMeasurements,
        string metricName,
        object? expectedValue = null,
        Dictionary<string, object?>? expectedTags = null,
        int minimumCount = 1)
    {
        ArgumentNullException.ThrowIfNull(capturedMeasurements);
        ArgumentNullException.ThrowIfNull(metricName);

        if (!capturedMeasurements.TryGetValue(metricName, out List<MeasurementCapture>? measurements))
        {
            string availableMetrics = string.Join(", ", capturedMeasurements.Keys);
            throw new InvalidOperationException($"Metric '{metricName}' not found. Available metrics: {availableMetrics}");
        }

        if (measurements.Count < minimumCount)
        {
            throw new InvalidOperationException(
                $"Metric '{metricName}' expected at least {minimumCount} measurements but found {measurements.Count}");
        }

        List<MeasurementCapture> matchingMeasurements = measurements;

        if (expectedValue != null)
        {
            matchingMeasurements = measurements.Where(m => Equals(m.Value, expectedValue)).ToList();
            if (matchingMeasurements.Count == 0)
            {
                string availableValues = string.Join(", ", measurements.Select(m => m.Value));
                throw new InvalidOperationException(
                    $"Metric '{metricName}' expected value '{expectedValue}' but found values: {availableValues}");
            }
        }

        if (expectedTags != null)
        {
            matchingMeasurements = matchingMeasurements.Where(m => TagsMatch(m.Tags, expectedTags)).ToList();
            if (matchingMeasurements.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Metric '{metricName}' no measurements found with expected tags");
            }
        }

        return matchingMeasurements;
    }

    /// <summary>
    /// Waits for a specific number of activities to be captured within a timeout period.
    /// </summary>
    /// <param name="capturedActivities">The list of captured activities.</param>
    /// <param name="expectedCount">The expected number of activities.</param>
    /// <param name="timeout">The timeout period (default: 5 seconds).</param>
    /// <returns>A task that completes when the expected count is reached or timeout occurs.</returns>
    public static async Task WaitForActivitiesAsync(
        IList<Activity> capturedActivities,
        int expectedCount,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(capturedActivities);

        TimeSpan actualTimeout = timeout ?? TimeSpan.FromSeconds(5);
        using CancellationTokenSource cts = new(actualTimeout);

        while (capturedActivities.Count < expectedCount && !cts.Token.IsCancellationRequested)
        {
            await Task.Delay(50, cts.Token).ConfigureAwait(false);
        }

        if (capturedActivities.Count < expectedCount)
        {
            throw new TimeoutException(
                $"Timeout waiting for {expectedCount} activities. Only {capturedActivities.Count} captured within {actualTimeout}");
        }
    }

    /// <summary>
    /// Waits for a specific metric to be recorded within a timeout period.
    /// </summary>
    /// <param name="capturedMeasurements">The captured measurements.</param>
    /// <param name="metricName">The expected metric name.</param>
    /// <param name="minimumCount">Minimum number of measurements to wait for (default: 1).</param>
    /// <param name="timeout">The timeout period (default: 5 seconds).</param>
    /// <returns>A task that completes when the metric is recorded or timeout occurs.</returns>
    public static async Task WaitForMetricAsync(
        Dictionary<string, List<MeasurementCapture>> capturedMeasurements,
        string metricName,
        int minimumCount = 1,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(capturedMeasurements);
        ArgumentNullException.ThrowIfNull(metricName);

        TimeSpan actualTimeout = timeout ?? TimeSpan.FromSeconds(5);
        using CancellationTokenSource cts = new(actualTimeout);

        while (!cts.Token.IsCancellationRequested)
        {
            if (capturedMeasurements.TryGetValue(metricName, out List<MeasurementCapture>? measurements) &&
                measurements.Count >= minimumCount)
            {
                return;
            }

            await Task.Delay(50, cts.Token).ConfigureAwait(false);
        }

        int actualCount = capturedMeasurements.TryGetValue(metricName, out List<MeasurementCapture>? final)
            ? final.Count : 0;
        throw new TimeoutException(
            $"Timeout waiting for metric '{metricName}' (minimum {minimumCount}). Only {actualCount} recorded within {actualTimeout}");
    }

    /// <summary>
    /// Creates a disposable test scope that sets up activity and meter listeners.
    /// </summary>
    /// <returns>A <see cref="TelemetryTestScope"/> for testing.</returns>
    public static TelemetryTestScope CreateTelemetryTestScope()
    {
        return new TelemetryTestScope();
    }

    private static void CaptureMeasurement<T>(
        Dictionary<string, List<MeasurementCapture>> capturedMeasurements,
        string instrumentName,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (!capturedMeasurements.TryGetValue(instrumentName, out List<MeasurementCapture>? measurements))
        {
            measurements = new List<MeasurementCapture>();
            capturedMeasurements[instrumentName] = measurements;
        }

        measurements.Add(new MeasurementCapture
        {
            Value = measurement,
            Tags = tags.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            Timestamp = DateTimeOffset.UtcNow,
        });
    }

    private static bool TagsMatch(Dictionary<string, object?> actualTags, Dictionary<string, object?> expectedTags)
    {
        return expectedTags.All(expected =>
            actualTags.TryGetValue(expected.Key, out object? actualValue) &&
            Equals(actualValue, expected.Value));
    }
}