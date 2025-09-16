// <copyright file="TenantOperationsBenchmarks.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.PerformanceTests.Benchmarks;

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Corvus.Tenancy;
using Marain.Tenancy.Storage.Azure.BlobStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

/// <summary>
/// Benchmarks for core tenant operations without telemetry overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[RPlotExporter]
public class TenantOperationsBenchmarks
{
    private ITenantStore? tenantStore;
    private ITenantProvider? tenantProvider;
    private readonly string testTenantId = "test-tenant-" + Guid.NewGuid().ToString("N")[..8];
    private readonly string parentTenantId = "parent-tenant-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Global setup for benchmarks - initialize services without telemetry.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        ServiceCollection services = new ServiceCollection();
        
        // Add logging without telemetry
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Mock storage for consistent performance testing
        this.tenantStore = Substitute.For<ITenantStore>();
        this.tenantProvider = Substitute.For<ITenantProvider>();
        
        // Setup mock responses for consistent benchmarking
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
    }

    /// <summary>
    /// Benchmark for getting a single tenant without telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark(Baseline = true)]
    public async Task<ITenant> GetTenant_NoTelemetry()
    {
        return await this.tenantProvider!.GetTenantAsync(this.testTenantId);
    }

    /// <summary>
    /// Benchmark for creating a child tenant without telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task<ITenant> CreateChildTenant_NoTelemetry()
    {
        return await this.tenantStore!.CreateWellKnownChildTenantAsync(
            this.parentTenantId, 
            Guid.NewGuid(), 
            "Child Tenant");
    }

    /// <summary>
    /// Benchmark for multiple sequential tenant operations without telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task MultipleOperations_NoTelemetry()
    {
        // Simulate a typical workflow: get parent, create child, get child
        ITenant parent = await this.tenantProvider!.GetTenantAsync(this.parentTenantId);
        ITenant child = await this.tenantStore!.CreateWellKnownChildTenantAsync(
            parent.Id, 
            Guid.NewGuid(), 
            "Workflow Child");
        ITenant retrieved = await this.tenantProvider.GetTenantAsync(child.Id);
    }

    /// <summary>
    /// Benchmark for concurrent tenant operations without telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [Benchmark]
    public async Task ConcurrentOperations_NoTelemetry()
    {
        const int concurrency = 10;
        Task<ITenant>[] tasks = new Task<ITenant>[concurrency];
        
        for (int i = 0; i < concurrency; i++)
        {
            tasks[i] = this.tenantProvider!.GetTenantAsync(this.testTenantId);
        }
        
        await Task.WhenAll(tasks);
    }
}