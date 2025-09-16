// <copyright file="TelemetryOverheadBenchmarks.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.PerformanceTests.Benchmarks;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Corvus.Tenancy;
using Marain.Tenancy.Shared.Extensions;
using Marain.Tenancy.Shared.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using OpenTelemetry.Metrics;

/// <summary>
/// Benchmarks to measure telemetry overhead on tenant operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[RPlotExporter]
public class TelemetryOverheadBenchmarks
{
    private ITenantStore? tenantStore;
    private ITenantProvider? tenantProvider;
    private ActivitySource? activitySource;
    private Meter? meter;
    private IServiceProvider? serviceProvider;
    private readonly string testTenantId = "test-tenant-" + Guid.NewGuid().ToString("N")[..8];
    private readonly string parentTenantId = "parent-tenant-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Global setup for telemetry-enabled benchmarks.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Configure basic services
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
            
        services.AddSingleton(configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Add telemetry with console exporters for minimal overhead
        services.AddMarainTelemetry(configuration);
        
        // Mock storage and provider for consistent testing
        this.tenantStore = Substitute.For<ITenantStore>();
        this.tenantProvider = Substitute.For<ITenantProvider>();
        
        ITenant mockTenant = Substitute.For<ITenant>();
        mockTenant.Id.Returns(this.testTenantId);
        mockTenant.Name.Returns("Test Tenant");
        
        ITenant mockParent = Substitute.For<ITenant>();
        mockParent.Id.Returns(this.parentTenantId);
        
        this.tenantProvider.GetTenantAsync(this.testTenantId).Returns(Task.FromResult(mockTenant));
        this.tenantProvider.GetTenantAsync(this.parentTenantId).Returns(Task.FromResult(mockParent));
        
        this.tenantStore.GetTenantAsync(this.testTenantId).Returns(Task.FromResult(mockTenant));
        this.tenantStore.CreateWellKnownChildTenantAsync(
            Arg.Any<string>(), 
            Arg.Any<Guid>(), 
            Arg.Any<string>()).Returns(Task.FromResult(mockTenant));
        
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
        (this.serviceProvider as IDisposable)?.Dispose();
    }

    /// <summary>
    /// Benchmark for getting a tenant with full telemetry instrumentation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task<ITenant> GetTenant_WithTelemetry()
    {
        using Activity? activity = this.activitySource!.StartActivity("tenant.get");
        activity?.SetTenantOperationTags(TelemetryConstants.OperationTypes.Get, this.testTenantId);
        
        try
        {
            ITenant result = await this.tenantProvider!.GetTenantAsync(this.testTenantId);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Benchmark for creating a child tenant with full telemetry instrumentation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task<ITenant> CreateChildTenant_WithTelemetry()
    {
        using Activity? activity = this.activitySource!.StartActivity("tenant.create-child");
        activity?.SetTag(TelemetryConstants.AttributeKeys.ParentTenantId, this.parentTenantId);
        activity?.SetTag(TelemetryConstants.AttributeKeys.OperationType, TelemetryConstants.OperationTypes.Create);
        
        // Add metric recording
        Counter<long> counter = this.meter!.CreateCounter<long>("tenant.operations.total");
        
        try
        {
            ITenant result = await this.tenantStore!.CreateWellKnownChildTenantAsync(
                this.parentTenantId, 
                Guid.NewGuid(), 
                "Child Tenant");
            
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

    /// <summary>
    /// Benchmark for multiple operations with telemetry correlation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task MultipleOperations_WithTelemetry()
    {
        using Activity? parentActivity = this.activitySource!.StartActivity("tenant.workflow");
        parentActivity?.SetTag("workflow.type", "create-and-retrieve");
        
        try
        {
            // Get parent tenant
            using (Activity? getActivity = this.activitySource.StartActivity("tenant.get-parent"))
            {
                getActivity?.SetTenantOperationTags(TelemetryConstants.OperationTypes.Get, this.parentTenantId);
                ITenant parent = await this.tenantProvider!.GetTenantAsync(this.parentTenantId);
                getActivity?.SetStatus(ActivityStatusCode.Ok);
            }
            
            // Create child tenant
            using (Activity? createActivity = this.activitySource.StartActivity("tenant.create-child"))
            {
                createActivity?.SetTag(TelemetryConstants.AttributeKeys.ParentTenantId, this.parentTenantId);
                ITenant child = await this.tenantStore!.CreateWellKnownChildTenantAsync(
                    this.parentTenantId, 
                    Guid.NewGuid(), 
                    "Workflow Child");
                createActivity?.SetStatus(ActivityStatusCode.Ok);
            }
            
            // Retrieve child tenant
            using (Activity? retrieveActivity = this.activitySource.StartActivity("tenant.get-child"))
            {
                retrieveActivity?.SetTenantOperationTags(TelemetryConstants.OperationTypes.Get, this.testTenantId);
                ITenant retrieved = await this.tenantProvider.GetTenantAsync(this.testTenantId);
                retrieveActivity?.SetStatus(ActivityStatusCode.Ok);
            }
            
            parentActivity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            parentActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Benchmark for measuring Activity creation overhead only.
    /// </summary>
    [Benchmark]
    public void ActivityCreation_Overhead()
    {
        using Activity? activity = this.activitySource!.StartActivity("test.operation");
        activity?.SetTag("test.key", "test.value");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Benchmark for measuring Meter operation overhead only.
    /// </summary>
    [Benchmark]
    public void MeterRecording_Overhead()
    {
        Counter<long> counter = this.meter!.CreateCounter<long>("test.counter");
        counter.Add(1, new KeyValuePair<string, object?>("test.tag", "test.value"));
    }

    /// <summary>
    /// Benchmark comparing sync vs async telemetry patterns.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task TelemetryPatterns_Comparison()
    {
        // Test different telemetry patterns for performance impact
        using Activity? activity = this.activitySource!.StartActivity("pattern.test");
        
        // Pattern 1: Minimal telemetry
        activity?.SetTag("operation", "test");
        
        // Pattern 2: Extended telemetry
        activity?.SetTag("tenant.id", this.testTenantId);
        activity?.SetTag("operation.type", "performance-test");
        activity?.SetTag("test.timestamp", DateTimeOffset.UtcNow.ToString("O"));
        
        // Simulate async work
        await Task.Yield();
        
        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}