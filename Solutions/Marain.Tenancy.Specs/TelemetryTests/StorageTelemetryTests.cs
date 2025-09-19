// <copyright file="StorageTelemetryTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.TelemetryTests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Corvus.Json;
using Corvus.Json.Serialization;
using Corvus.Storage.Azure.BlobStorage;
using Corvus.Storage.Azure.BlobStorage.Tenancy;
using Corvus.Tenancy;
using Marain.Tenancy.Shared.Telemetry;
using Marain.Tenancy.Shared.Testing;
using Marain.Tenancy.Storage.Azure.BlobStorage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

/// <summary>
/// Tests for telemetry in the storage layer.
/// </summary>
[TestFixture]
public class StorageTelemetryTests
{
    private TelemetryTestScope? telemetryScope;
    private AzureBlobStorageTenantStore? tenantStore;

    /// <summary>
    /// Sets up the test environment before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this.telemetryScope = TelemetryTestHelpers.CreateTelemetryTestScope();

        // Create test dependencies (these would normally be injected)
        AzureBlobStorageTenantStoreConfiguration config = new(
            new BlobContainerConfiguration(),
            false);

        // Note: In real tests, you would use test doubles or a test container
        // This is a simplified example showing the telemetry testing pattern
        this.tenantStore = new AzureBlobStorageTenantStore(
            config,
            MockBlobContainerSource(),
            MockSerializerOptionsProvider(),
            MockPropertyBagFactory(),
            NullLogger<AzureBlobStorageTenantStore>.Instance);
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
    /// Tests that creating a tenant generates the expected telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CreateWellKnownChildTenantAsync_ShouldGenerateCorrectTelemetry()
    {
        // Arrange
        const string parentTenantId = "parent-tenant-123";
        var childTenantGuid = Guid.NewGuid();
        const string tenantName = "Test Tenant";

        // Act
        try
        {
            await this.tenantStore!.CreateWellKnownChildTenantAsync(parentTenantId, childTenantGuid, tenantName);
        }
        catch
        {
            // Expected to fail in unit test environment - we're testing telemetry, not functionality
        }

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(1, TimeSpan.FromSeconds(2));

        // Assert - Verify activity was created
        Activity activity = this.telemetryScope.ValidateActivity(
            "storage.tenant.create",
            new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.StorageOperation] = TelemetryConstants.OperationTypes.Create,
                [TelemetryConstants.AttributeKeys.TenantId] = parentTenantId,
                [TelemetryConstants.AttributeKeys.AzureContainer] = "corvustenancy",
                [TelemetryConstants.AttributeKeys.ChildTenantGuid] = childTenantGuid.ToString(),
                [TelemetryConstants.AttributeKeys.TenantName] = tenantName,
            });

        Assert.That(activity, Is.Not.Null);
        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Error)); // Expected due to test environment
    }

    /// <summary>
    /// Tests that getting a tenant generates the expected telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GetTenantAsync_RootTenant_ShouldGenerateCorrectTelemetry()
    {
        // Arrange
        string tenantId = RootTenant.RootTenantId;

        // Act
        try
        {
            await this.tenantStore!.GetTenantAsync(tenantId);
        }
        catch
        {
            // Expected to fail in unit test environment
        }

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(1, TimeSpan.FromSeconds(2));

        // Assert - Verify activity was created
        this.telemetryScope.ValidateActivity(
            "storage.tenant.get",
            new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.StorageOperation] = TelemetryConstants.OperationTypes.Get,
                [TelemetryConstants.AttributeKeys.TenantId] = tenantId,
                [TelemetryConstants.AttributeKeys.AzureContainer] = "corvustenancy",
            });
    }

    /// <summary>
    /// Tests that deleting a tenant generates the expected telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task DeleteTenantAsync_ShouldGenerateCorrectTelemetry()
    {
        // Arrange
        const string tenantId = "tenant-to-delete-123";

        // Act
        try
        {
            await this.tenantStore!.DeleteTenantAsync(tenantId);
        }
        catch
        {
            // Expected to fail in unit test environment
        }

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(1, TimeSpan.FromSeconds(2));

        // Assert - Verify activity and metrics
        this.telemetryScope.ValidateActivity(
            "storage.tenant.delete",
            new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.StorageOperation] = TelemetryConstants.OperationTypes.Delete,
                [TelemetryConstants.AttributeKeys.TenantId] = tenantId,
                [TelemetryConstants.AttributeKeys.AzureContainer] = "corvustenancy",
            },
            ActivityStatusCode.Error); // Expected due to test environment

        // Verify error metrics were recorded
        this.telemetryScope.ValidateMetric(
            "tenant.storage.errors.total",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Delete,
            });

        this.telemetryScope.ValidateMetric(
            "tenant.storage.operations.total",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.OperationType] = TelemetryConstants.OperationTypes.Delete,
                ["status"] = "error",
            });
    }

    /// <summary>
    /// Tests that getting children generates the expected telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GetChildrenAsync_ShouldGenerateCorrectTelemetry()
    {
        // Arrange
        const string tenantId = "parent-tenant-456";
        int limit = 20;

        // Act
        try
        {
            await this.tenantStore!.GetChildrenAsync(tenantId, limit, null);
        }
        catch
        {
            // Expected to fail in unit test environment
        }

        // Wait for telemetry to be captured
        await this.telemetryScope!.WaitForActivitiesAsync(1, TimeSpan.FromSeconds(2));

        // Assert
        this.telemetryScope.ValidateActivity(
            "storage.tenant.list-children",
            new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.StorageOperation] = TelemetryConstants.OperationTypes.List,
                [TelemetryConstants.AttributeKeys.TenantId] = tenantId,
                [TelemetryConstants.AttributeKeys.AzureContainer] = "corvustenancy",
            });
    }

    // Mock implementations for testing
    private static IBlobContainerSourceWithTenantLegacyTransition MockBlobContainerSource()
    {
        // In real tests, you would return a proper mock or test double
        return Mock.Of<IBlobContainerSourceWithTenantLegacyTransition>();
    }

    private static IJsonSerializerOptionsProvider MockSerializerOptionsProvider()
    {
        // In real tests, you would return a proper mock or test double
        return Mock.Of<IJsonSerializerOptionsProvider>();
    }

    private static IPropertyBagFactory MockPropertyBagFactory()
    {
        // In real tests, you would return a proper mock or test double
        var mockFactory = new Mock<IPropertyBagFactory>();
        var mockBag = new Mock<IPropertyBag>();

        mockFactory.Setup(x => x.Create(It.IsAny<IEnumerable<KeyValuePair<string, object>>>())).Returns(mockBag.Object);
        mockFactory.Setup(
            x => x.CreateModified(
                It.IsAny<IPropertyBag>(),
                It.IsAny<IEnumerable<KeyValuePair<string, object>>?>(),
                It.IsAny<IEnumerable<string>?>())).Returns(mockBag.Object);

        return mockFactory.Object;
    }
}