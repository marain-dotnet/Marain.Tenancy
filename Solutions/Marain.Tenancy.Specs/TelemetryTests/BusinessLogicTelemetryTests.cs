// <copyright file="BusinessLogicTelemetryTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.TelemetryTests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Corvus.Json;
using Corvus.Tenancy;
using Corvus.Tenancy.Exceptions;
using Marain.Clients;
using Marain.Tenancy;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Resources;
using Marain.Tenancy.Mappers;
using Marain.Tenancy.Shared.Telemetry;
using Marain.Tenancy.Shared.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

/// <summary>
/// Tests for telemetry in the business logic layer.
/// </summary>
[TestFixture]
public class BusinessLogicTelemetryTests
{
    private const string ValidTenantId = "b50ec3fc29514648abe4947afe833697";

    private TelemetryTestScope? telemetryScope;
    private ClientTenantProvider? tenantProvider;
    private ClientTenantStore? tenantStore;
    private ITenancyClient mockApiClient = null!;
    private ITenantMapper mockTenantMapper = null!;
    private IPropertyBagFactory mockPropertyBagFactory = null!;
    private RootTenant rootTenant = null!;

    /// <summary>
    /// Sets up the test environment before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this.telemetryScope = TelemetryTestHelpers.CreateTelemetryTestScope();

        // Create mock dependencies
        this.mockApiClient = Substitute.For<ITenancyClient>();
        this.mockTenantMapper = Substitute.For<ITenantMapper>();
        this.mockPropertyBagFactory = Substitute.For<IPropertyBagFactory>();

        // Create root tenant
        this.rootTenant = new RootTenant(this.mockPropertyBagFactory);

        // Create systems under test with telemetry
        this.tenantProvider = new ClientTenantProvider(
            this.rootTenant,
            this.mockApiClient,
            this.mockTenantMapper,
            NullLogger<ClientTenantProvider>.Instance);

        this.tenantStore = new ClientTenantStore(
            this.rootTenant,
            this.mockApiClient,
            this.mockTenantMapper,
            this.mockPropertyBagFactory,
            NullLogger<ClientTenantStore>.Instance);
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
    /// Tests that getting root tenant generates correct telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GetTenantAsync_RootTenant_ShouldGenerateCorrectTelemetry()
    {
        // Arrange
        string tenantId = RootTenant.RootTenantId;

        // Act
        ITenant result = await this.tenantProvider!.GetTenantAsync(tenantId);

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(1, TimeSpan.FromSeconds(2));

        // Assert
        Assert.That(result, Is.EqualTo(this.rootTenant));

        Activity activity = this.telemetryScope.ValidateActivity(
            "business.tenant.get",
            new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Get,
                [TelemetryConstants.AttributeKeys.TenantId] = tenantId,
            },
            ActivityStatusCode.Ok);

        Assert.That(activity, Is.Not.Null);

        // Verify success metrics
        this.telemetryScope.ValidateMetric(
            "tenant.business.operations.total",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Get,
                ["status"] = "success",
            });
    }

    /// <summary>
    /// Tests that getting a non-root tenant via API generates correct telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GetTenantAsync_NonRootTenant_Success_ShouldGenerateCorrectTelemetry()
    {
        // Arrange
        const string tenantId = "test-tenant-123";
        const string eTag = "test-etag";

        ApiResponse<TenantResource> mockResponse = new(
            HttpStatusCode.OK,
            new Dictionary<string, string?> { ["etag"] = eTag }.ToImmutableDictionary(),
            new TenantResource { Id = tenantId, Name = "Test Tenant" });

        ITenant mockTenant = Substitute.For<ITenant>();
        mockTenant.Id.Returns(tenantId);

        this.mockApiClient.GetTenantAsync(tenantId, eTag).Returns(Task.FromResult(mockResponse));
        this.mockTenantMapper.MapTenant(mockResponse.Body, eTag).Returns(mockTenant);

        // Act
        ITenant result = await this.tenantProvider!.GetTenantAsync(tenantId, eTag);

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(1, TimeSpan.FromSeconds(2));

        // Assert
        Assert.That(result, Is.EqualTo(mockTenant));

        Activity activity = this.telemetryScope.ValidateActivity(
            "business.tenant.get",
            new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Get,
                [TelemetryConstants.AttributeKeys.TenantId] = tenantId,
                ["etag"] = eTag,
            },
            ActivityStatusCode.Ok);

        Assert.That(activity, Is.Not.Null);

        // Verify success metrics
        this.telemetryScope.ValidateMetric(
            "tenant.business.operations.total",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Get,
                ["status"] = "success",
            });
    }

    /// <summary>
    /// Tests that tenant not found generates correct error telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GetTenantAsync_TenantNotFound_ShouldGenerateErrorTelemetry()
    {
        // Arrange
        const string tenantId = "nonexistent-tenant-123";

        this.mockApiClient.GetTenantAsync(tenantId, null)
            .ThrowsForAnyArgs(new MarainApiException("Tenant not found") { StatusCode = HttpStatusCode.NotFound });

        // Act & Assert
        Assert.ThrowsAsync<TenantNotFoundException>(
            async () => await this.tenantProvider!.GetTenantAsync(tenantId).ConfigureAwait(false));

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(1, TimeSpan.FromSeconds(2));

        // Verify error activity
        Activity activity = this.telemetryScope.ValidateActivity(
            "business.tenant.get",
            new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Get,
                [TelemetryConstants.AttributeKeys.TenantId] = tenantId,
            },
            ActivityStatusCode.Error);

        Assert.That(activity, Is.Not.Null);

        // Verify error metrics
        this.telemetryScope.ValidateMetric(
            "tenant.business.errors.total",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Get,
                ["error.type"] = "not_found",
            });

        this.telemetryScope.ValidateMetric(
            "tenant.business.operations.total",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Get,
                ["status"] = "error",
            });
    }

    /// <summary>
    /// Tests that creating a child tenant generates correct telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CreateChildTenantAsync_Success_ShouldGenerateCorrectTelemetry()
    {
        // Arrange
        const string parentTenantId = "parent-tenant-456";
        const string tenantName = "New Child Tenant";
        const string newTenantId = "new-child-tenant-789";

        ApiResponse<TenantResource> mockResponse = new(
            HttpStatusCode.OK,
            new Dictionary<string, string?> { ["etag"] = "new-etag" }.ToImmutableDictionary(),
            new TenantResource { Id = newTenantId, Name = tenantName });

        ITenant mockTenant = Substitute.For<ITenant>();
        mockTenant.Id.Returns(newTenantId);

        this.mockApiClient.CreateChildTenantAsync(parentTenantId, tenantName, Arg.Any<string>())
            .Returns(Task.FromResult(mockResponse));
        this.mockTenantMapper.MapTenant(mockResponse.Body, "new-etag").Returns(mockTenant);

        // Act
        ITenant result = await this.tenantStore!.CreateChildTenantAsync(parentTenantId, tenantName);

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(1, TimeSpan.FromSeconds(2));

        // Assert
        Assert.That(result, Is.EqualTo(mockTenant));

        Activity activity = this.telemetryScope.ValidateActivity(
            "store.tenant.create-child",
            new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Create,
                [TelemetryConstants.AttributeKeys.TenantId] = parentTenantId,
                [TelemetryConstants.AttributeKeys.TenantName] = tenantName,
            },
            ActivityStatusCode.Ok);

        Assert.That(activity, Is.Not.Null);

        // Verify success metrics
        this.telemetryScope.ValidateMetric(
            "tenant.store.operations.total",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Create,
                ["status"] = "success",
            });
    }

    /// <summary>
    /// Tests that tenant conflict during creation generates correct error telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CreateChildTenantAsync_Conflict_ShouldGenerateErrorTelemetry()
    {
        // Arrange
        const string parentTenantId = "parent-tenant-456";
        const string tenantName = "Conflicting Tenant";

        this.mockApiClient.CreateChildTenantAsync(parentTenantId, tenantName, Arg.Any<string>())
            .ThrowsForAnyArgs(new MarainApiException("Tenant already exists") { StatusCode = HttpStatusCode.Conflict });

        // Act & Assert
        Assert.ThrowsAsync<TenantConflictException>(
            async () => await this.tenantStore!.CreateChildTenantAsync(parentTenantId, tenantName).ConfigureAwait(false));

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(1, TimeSpan.FromSeconds(2));

        // Verify error activity
        Activity activity = this.telemetryScope.ValidateActivity(
            "store.tenant.create-child",
            new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Create,
                [TelemetryConstants.AttributeKeys.TenantId] = parentTenantId,
                [TelemetryConstants.AttributeKeys.TenantName] = tenantName,
            },
            ActivityStatusCode.Error);

        Assert.That(activity, Is.Not.Null);

        // Verify error metrics
        this.telemetryScope.ValidateMetric(
            "tenant.store.errors.total",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Create,
                ["error.type"] = "conflict",
            });
    }

    /// <summary>
    /// Tests that operation duration metrics are recorded for business operations.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task BusinessOperations_ShouldRecordDurationMetrics()
    {
        // Arrange & Act - Perform operations
        await this.tenantProvider!.GetTenantAsync(RootTenant.RootTenantId);

        // Wait for metrics to be captured
        await this.telemetryScope!.WaitForMetricAsync("tenant.business.operation.duration", timeout: TimeSpan.FromSeconds(2));

        // Assert
        List<MeasurementCapture> measurements = this.telemetryScope.ValidateMetric("tenant.business.operation.duration");

        Assert.That(measurements, Is.Not.Empty);
        Assert.That(measurements[0].Value, Is.TypeOf<double>());
        Assert.That((double)measurements[0].Value!, Is.GreaterThan(0));
    }

    /// <summary>
    /// Tests that delete operations generate correct telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task DeleteTenantAsync_Success_ShouldGenerateCorrectTelemetry()
    {
        // Mock the GetParentId extension method behavior
        // In a real test, you'd need to set up the tenant hierarchy properly

        // Act
        await this.tenantStore!.DeleteTenantAsync(ValidTenantId);

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(1, TimeSpan.FromSeconds(2));

        // Assert
        Activity activity = this.telemetryScope.ValidateActivity(
            "store.tenant.delete",
            new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Delete,
                [TelemetryConstants.AttributeKeys.TenantId] = ValidTenantId,
            },
            ActivityStatusCode.Ok);

        Assert.That(activity, Is.Not.Null);

        // Verify success metrics
        this.telemetryScope.ValidateMetric(
            "tenant.store.operations.total",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Delete,
                ["status"] = "success",
            });
    }

    /// <summary>
    /// Tests that all operations record duration metrics.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task AllStoreOperations_ShouldRecordDurationMetrics()
    {
        // Arrange & Act - Perform multiple operations
        await this.tenantProvider!.GetTenantAsync(RootTenant.RootTenantId);
        await this.tenantStore!.DeleteTenantAsync(ValidTenantId);

        // Wait for metrics to be captured
        await Task.Delay(100); // Allow metrics to be processed

        // Assert - Both business and store duration metrics should be recorded
        this.telemetryScope!.ValidateMetric("tenant.business.operation.duration", minimumCount: 1);
        this.telemetryScope.ValidateMetric("tenant.store.operation.duration", minimumCount: 1);
    }
}