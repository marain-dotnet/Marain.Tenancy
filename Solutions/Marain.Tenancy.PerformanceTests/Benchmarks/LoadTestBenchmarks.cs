// <copyright file="LoadTestBenchmarks.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.PerformanceTests.Benchmarks;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Corvus.Tenancy;
using Marain.Tenancy.Shared.Extensions;
using Marain.Tenancy.Shared.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenTelemetry.Metrics;

/// <summary>
/// Load testing benchmarks to validate telemetry performance under high-volume scenarios.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[RPlotExporter]
public class LoadTestBenchmarks
{
    private ITenantStore? tenantStore;
    private ITenantProvider? tenantProvider;
    private ActivitySource? activitySource;
    private Meter? meter;
    private IServiceProvider? serviceProvider;
    private readonly ConcurrentBag<string> testTenantIds = new();
    private readonly SemaphoreSlim rateLimitSemaphore = new(100, 100); // Limit to 100 concurrent operations

    /// <summary>
    /// Parameters for concurrent operation counts.
    /// </summary>
    [Params(10, 50, 100, 500)]
    public int ConcurrentOperations { get; set; }

    /// <summary>
    /// Global setup for load testing benchmarks.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Configure basic services
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenTelemetry:TraceConfig:Sampler"] = "ParentBased(TraceIdRatio(0.1))", // 10% sampling for load tests
            })
            .Build();
            
        services.AddSingleton(configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Error));
        
        // Add telemetry with sampling to reduce overhead during load testing
        services.AddMarainTelemetry(configuration);
        
        // Mock storage and provider for consistent load testing
        this.tenantStore = Substitute.For<ITenantStore>();
        this.tenantProvider = Substitute.For<ITenantProvider>();
        
        // Pre-populate test tenant IDs
        for (int i = 0; i < 1000; i++)
        {
            this.testTenantIds.Add($"load-test-tenant-{i:D4}");
        }
        
        // Setup mock responses with realistic delays
        this.tenantProvider.GetTenantAsync(Arg.Any<string>()).Returns(callInfo =>
        {
            string tenantId = callInfo.ArgAt<string>(0);
            ITenant mockTenant = Substitute.For<ITenant>();
            mockTenant.Id.Returns(tenantId);
            mockTenant.Name.Returns($"Tenant {tenantId}");
            
            // Simulate realistic database latency (1-5ms)
            return Task.Delay(Random.Shared.Next(1, 6)).ContinueWith(_ => mockTenant);
        });
        
        this.tenantStore.CreateWellKnownChildTenantAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                ITenant mockTenant = Substitute.For<ITenant>();
                mockTenant.Id.Returns(Guid.NewGuid().ToString());
                mockTenant.Name.Returns(callInfo.ArgAt<string>(2));
                
                // Simulate realistic creation latency (5-20ms)
                return Task.Delay(Random.Shared.Next(5, 21)).ContinueWith(_ => mockTenant);
            });
        
        services.AddSingleton(this.tenantStore);
        services.AddSingleton(this.tenantProvider);
        
        this.serviceProvider = services.BuildServiceProvider();
        
        // Initialize telemetry components
        this.activitySource = new ActivitySource(TelemetryConstants.BusinessActivitySource);
        this.meter = new Meter(TelemetryConstants.TenancyMeter);
    }

    /// <summary>
    /// Cleanup resources after benchmarks.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        this.activitySource?.Dispose();
        this.meter?.Dispose();
        this.rateLimitSemaphore.Dispose();
        (this.serviceProvider as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Load test for concurrent tenant retrieval operations with telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task ConcurrentTenantRetrievals_WithTelemetry()
    {
        var tasks = new Task[this.ConcurrentOperations];
        string[] tenantIds = this.testTenantIds.Take(this.ConcurrentOperations).ToArray();
        
        for (int i = 0; i < this.ConcurrentOperations; i++)
        {
            string tenantId = tenantIds[i];
            tasks[i] = this.ExecuteGetTenantWithTelemetry(tenantId);
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Load test for concurrent tenant creation operations with telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task ConcurrentTenantCreations_WithTelemetry()
    {
        var tasks = new Task<ITenant>[this.ConcurrentOperations];
        string[] tenantIds = this.testTenantIds.Take(this.ConcurrentOperations).ToArray();
        
        for (int i = 0; i < this.ConcurrentOperations; i++)
        {
            string parentId = tenantIds[i];
            tasks[i] = this.ExecuteCreateChildTenantWithTelemetry(parentId, $"Child-{i:D4}");
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Load test for mixed operations (read-heavy workload) with telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task MixedOperations_ReadHeavy_WithTelemetry()
    {
        var tasks = new Task[this.ConcurrentOperations];
        string[] tenantIds = this.testTenantIds.Take(this.ConcurrentOperations).ToArray();
        
        for (int i = 0; i < this.ConcurrentOperations; i++)
        {
            string tenantId = tenantIds[i];
            
            // 80% reads, 20% writes
            if (i % 5 == 0)
            {
                tasks[i] = this.ExecuteCreateChildTenantWithTelemetry(tenantId, $"Mixed-Child-{i:D4}").ContinueWith(t => t.Result as object);
            }
            else
            {
                tasks[i] = this.ExecuteGetTenantWithTelemetry(tenantId).ContinueWith(t => t.Result as object);
            }
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Stress test for telemetry system under sustained load.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task SustainedLoad_TelemetryStressTest()
    {
        const int durationSeconds = 5;
        const int operationsPerSecond = 100;
        
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds));
        var tasks = new List<Task>();
        
        // Generate sustained load
        while (!cts.Token.IsCancellationRequested)
        {
            for (int i = 0; i < operationsPerSecond && !cts.Token.IsCancellationRequested; i++)
            {
                string tenantId = this.testTenantIds.Skip(Random.Shared.Next(0, 500)).First();
                tasks.Add(this.ExecuteGetTenantWithTelemetry(tenantId));
                
                // Rate limiting to prevent resource exhaustion
                await this.rateLimitSemaphore.WaitAsync(cts.Token);
            }
            
            // Brief pause between bursts
            if (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(100, cts.Token);
            }
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmark for telemetry export buffer behavior under high load.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task TelemetryExportBuffer_HighLoad()
    {
        // Generate a burst of telemetry data to test export buffer handling
        var tasks = new Task[1000];
        Counter<long> counter = this.meter!.CreateCounter<long>("load.test.operations");
        
        for (int i = 0; i < 1000; i++)
        {
            int operationId = i;
            tasks[i] = Task.Run(() =>
            {
                using Activity? activity = this.activitySource!.StartActivity($"load.test.{operationId:D4}");
                activity?.SetTag("operation.id", operationId.ToString());
                activity?.SetTag("batch.type", "export-buffer-test");
                activity?.SetStatus(ActivityStatusCode.Ok);
                
                counter.Add(1, new KeyValuePair<string, object?>("test.type", "export-buffer"));
            });
        }
        
        await Task.WhenAll(tasks);
        
        // Small delay to allow telemetry export processing
        await Task.Delay(100);
    }

    private async Task<ITenant> ExecuteGetTenantWithTelemetry(string tenantId)
    {
        await this.rateLimitSemaphore.WaitAsync();
        try
        {
            using Activity? activity = this.activitySource!.StartActivity("load.test.get-tenant");
            activity?.SetTenantOperationTags(TelemetryConstants.OperationTypes.Get, tenantId);
            
            try
            {
                ITenant result = await this.tenantProvider!.GetTenantAsync(tenantId);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return result;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
        finally
        {
            this.rateLimitSemaphore.Release();
        }
    }

    private async Task<ITenant> ExecuteCreateChildTenantWithTelemetry(string parentId, string childName)
    {
        await this.rateLimitSemaphore.WaitAsync();
        try
        {
            using Activity? activity = this.activitySource!.StartActivity("load.test.create-tenant");
            activity?.SetTag(TelemetryConstants.AttributeKeys.ParentTenantId, parentId);
            activity?.SetTag(TelemetryConstants.AttributeKeys.TenantName, childName);
            activity?.SetTag(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Create);

            Counter<long> counter = this.meter!.CreateCounter<long>("load.test.operations");
            
            try
            {
                ITenant result = await this.tenantStore!.CreateWellKnownChildTenantAsync(
                    parentId, 
                    Guid.NewGuid(), 
                    childName);
                
                activity?.SetStatus(ActivityStatusCode.Ok);
                counter.Add(1, new KeyValuePair<string, object?>("operation", "create"));
                
                return result;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                counter.Add(1, 
                    new KeyValuePair<string, object?>("operation", "create"),
                    new KeyValuePair<string, object?>("status", "error"));
                throw;
            }
        }
        finally
        {
            this.rateLimitSemaphore.Release();
        }
    }
}