// <copyright file="MeasurementCapture.cs" company="Endjin Limited">
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
/// Represents a captured metric measurement for testing.
/// </summary>
public class MeasurementCapture
{
    /// <summary>
    /// Gets or sets the measured value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the measurement tags.
    /// </summary>
    public Dictionary<string, object?> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the measurement was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}