// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.PerformanceTests;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

/// <summary>
/// Entry point for the performance testing application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point for the benchmark runner.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
    {
        // Create a custom configuration for our benchmarks
        IConfig config = ManualConfig.Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        // Run all benchmarks in this assembly
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
    }
}