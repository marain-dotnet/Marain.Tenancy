// <copyright file="CliCommandsTelemetryTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.TelemetryTests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Corvus.Json.Serialization;
using Corvus.Tenancy;
using Marain.Tenancy.Cli.Commands;
using Marain.Tenancy.Shared.Telemetry;
using Marain.Tenancy.Shared.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Spectre.Console.Cli;

/// <summary>
/// Unit tests for CLI commands telemetry functionality.
/// </summary>
[TestFixture]
[Category("Telemetry")]
public class CliCommandsTelemetryTests
{
    private TelemetryTestScope? telemetryScope;
    private Mock<ITenantProvider>? mockTenantProvider;
    private Mock<ITenantStore>? mockTenantStore;
    private Mock<ILogger<Get>>? mockGetLogger;
    private Mock<ILogger<Create>>? mockCreateLogger;
    private Mock<ILogger<Delete>>? mockDeleteLogger;
    private Mock<ILogger<Cli.Commands.List>>? mockListLogger;

    /// <summary>
    /// Sets up the test environment before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this.telemetryScope = TelemetryTestHelpers.CreateTelemetryTestScope();
        this.mockTenantProvider = new Mock<ITenantProvider>();
        this.mockTenantStore = new Mock<ITenantStore>();
        this.mockGetLogger = new Mock<ILogger<Get>>();
        this.mockCreateLogger = new Mock<ILogger<Create>>();
        this.mockDeleteLogger = new Mock<ILogger<Delete>>();
        this.mockListLogger = new Mock<ILogger<Marain.Tenancy.Cli.Commands.List>>();

        // Setup common mock responses
        Mock<ITenant> mockTenant = new();
        Mock<RootTenant> mockRootTenant = new();
        mockTenant.SetupGet(x => x.Id).Returns("test-tenant-123");
        mockTenant.SetupGet(x => x.Name).Returns("Test Tenant");

        this.mockTenantProvider.Setup(x => x.GetTenantAsync(It.IsAny<string>(), It.IsAny<string?>())).Returns(Task.FromResult(mockTenant.Object));
        this.mockTenantProvider.Setup(x => x.Root).Returns(mockRootTenant.Object);

        this.mockTenantStore.Setup(x => x.CreateWellKnownChildTenantAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(Task.FromResult(mockTenant.Object));
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
    /// Tests that the Get command creates proper telemetry activities.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GetCommand_ShouldCreateTelemetryActivity()
    {
        // Arrange
        var serializerProvider = new Mock<IJsonSerializerOptionsProvider>();
        var getCommand = new Get(this.mockTenantProvider!.Object, serializerProvider.Object, this.mockGetLogger!.Object);
        var settings = new GetSettings { TenantId = "test-tenant-123" };
        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "get", null);

        // Act
        int result = await getCommand.ExecuteAsync(context, settings);

        // Assert
        await this.telemetryScope!.WaitForActivitiesAsync(1);

        Activity activity = this.telemetryScope.ValidateActivity(
            "cli.get-tenant",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.TenantId] = "test-tenant-123",
                [TelemetryConstants.AttributeKeys.CommandType] = TelemetryConstants.OperationTypes.Get,
            },
            expectedStatus: ActivityStatusCode.Ok);

        Assert.That(activity, Is.Not.Null);
        Assert.That(result, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the Get command handles errors properly with telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task GetCommand_Error_ShouldRecordErrorTelemetry()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Tenant not found");
        this.mockTenantProvider!.Setup(x => x.GetTenantAsync(It.IsAny<string>(), It.IsAny<string?>())).Throws(expectedException);

        Mock<IJsonSerializerOptionsProvider> serializerProvider = new();
        var getCommand = new Get(this.mockTenantProvider.Object, serializerProvider.Object, this.mockGetLogger!.Object);
        var settings = new GetSettings { TenantId = "nonexistent-tenant" };
        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "get", null);

        // Act & Assert
        InvalidOperationException? thrownException = Assert.ThrowsAsync<InvalidOperationException>(
            () => getCommand.ExecuteAsync(context, settings));

        Assert.That(thrownException?.Message, Is.EqualTo("Tenant not found"));

        await this.telemetryScope!.WaitForActivitiesAsync(1);

        Activity activity = this.telemetryScope.ValidateActivity(
            "cli.get-tenant",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.TenantId] = "nonexistent-tenant",
                [TelemetryConstants.AttributeKeys.CommandType] = TelemetryConstants.OperationTypes.Get,
            },
            expectedStatus: ActivityStatusCode.Error);

        Assert.That(activity, Is.Not.Null);
    }

    /// <summary>
    /// Tests that the Create command creates proper telemetry activities.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CreateCommand_ShouldCreateTelemetryActivity()
    {
        // Arrange
        var createCommand = new Create(this.mockTenantStore!.Object, this.mockCreateLogger!.Object);
        var settings = new CreateSettings
        {
            TenantId = "parent-tenant-123",
            Name = "New Child Tenant",
            WellKnownTenantGuid = Guid.NewGuid().ToString(),
        };
        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "create", null);

        // Act
        int result = await createCommand.ExecuteAsync(context, settings);

        // Assert
        await this.telemetryScope!.WaitForActivitiesAsync(1);

        Activity activity = this.telemetryScope.ValidateActivity(
            "cli.create-tenant",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.ParentTenantId] = "parent-tenant-123",
                [TelemetryConstants.AttributeKeys.TenantName] = "New Child Tenant",
                [TelemetryConstants.AttributeKeys.CommandType] = TelemetryConstants.OperationTypes.Create,
            },
            expectedStatus: ActivityStatusCode.Ok);

        Assert.That(activity, Is.Not.Null);
        Assert.That(result, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the Delete command creates proper telemetry activities.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task DeleteCommand_ShouldCreateTelemetryActivity()
    {
        // Arrange
        var deleteCommand = new Delete(this.mockTenantStore!.Object, this.mockDeleteLogger!.Object);
        var settings = new DeleteSettings { TenantId = "tenant-to-delete-123" };
        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "delete", null);

        // Mock successful deletion
        this.mockTenantStore.Setup(x => x.DeleteTenantAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        int result = await deleteCommand.ExecuteAsync(context, settings);

        // Assert
        await this.telemetryScope!.WaitForActivitiesAsync(1);

        Activity activity = this.telemetryScope.ValidateActivity(
            "cli.delete-tenant",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.TenantId] = "tenant-to-delete-123",
                [TelemetryConstants.AttributeKeys.CommandType] = TelemetryConstants.OperationTypes.Delete,
            },
            expectedStatus: ActivityStatusCode.Ok);

        Assert.That(activity, Is.Not.Null);
        Assert.That(result, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the List command creates proper telemetry activities.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ListCommand_ShouldCreateTelemetryActivity()
    {
        // Arrange
        var serializerProvider = new Mock<IJsonSerializerOptionsProvider>();
        var listCommand = new Marain.Tenancy.Cli.Commands.List(this.mockTenantStore!.Object, serializerProvider.Object, this.mockListLogger!.Object);
        var settings = new ListSettings { TenantId = "parent-tenant-123" };
        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "list", null);

        TenantCollectionResult tenantCollectionResult = new(["child1", "child2"], null);

        this.mockTenantStore!.Setup(x => x.GetChildrenAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>())).Returns(Task.FromResult(tenantCollectionResult));

        // Act
        int result = await listCommand.ExecuteAsync(context, settings);

        // Assert
        await this.telemetryScope!.WaitForActivitiesAsync(1);

        Activity activity = this.telemetryScope.ValidateActivity(
            "cli.list-tenants",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.ParentTenantId] = "parent-tenant-123",
                [TelemetryConstants.AttributeKeys.CommandType] = TelemetryConstants.OperationTypes.List,
            },
            expectedStatus: ActivityStatusCode.Ok);

        Assert.That(activity, Is.Not.Null);
        Assert.That(tenantCollectionResult, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that CLI commands support activity correlation.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CliCommands_ShouldSupportActivityCorrelation()
    {
        // Arrange
        using var testSource = new ActivitySource("test.correlation");
        using Activity? parentActivity = testSource.StartActivity("test.parent-operation");
        parentActivity?.SetTag("test.scenario", "cli-correlation");

        Mock<IJsonSerializerOptionsProvider> serializerProvider = new();
        var getCommand = new Get(this.mockTenantProvider!.Object, serializerProvider.Object, this.mockGetLogger!.Object);
        var settings = new GetSettings { TenantId = "correlation-test-tenant" };
        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "get", null);

        // Act
        int result = await getCommand.ExecuteAsync(context, settings);

        // Assert
        await this.telemetryScope!.WaitForActivitiesAsync(2); // Parent + child activities

        List<Activity> activities = this.telemetryScope.CapturedActivities;
        Assert.That(activities.Count, Is.EqualTo(2));

        // Find the CLI activity
        Activity? cliActivity = activities.Find(a => a.DisplayName == "cli.get-tenant");
        Assert.That(cliActivity, Is.Not.Null);
        Assert.That(cliActivity!.ParentId, Is.EqualTo(parentActivity?.Id));
        Assert.That(result, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that CLI commands record appropriate metrics.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CliCommands_ShouldRecordMetrics()
    {
        // Arrange
        var createCommand = new Create(this.mockTenantStore!.Object, this.mockCreateLogger!.Object);
        var settings = new CreateSettings
        {
            TenantId = "parent-metrics-test",
            Name = "Metrics Test Tenant",
            WellKnownTenantGuid = Guid.NewGuid().ToString(),
        };
        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "create", null);

        // Act
        int result = await createCommand.ExecuteAsync(context, settings);

        // Assert
        await this.telemetryScope!.WaitForMetricAsync("cli.commands.total");

        List<MeasurementCapture> measurements = this.telemetryScope.ValidateMetric(
            "cli.commands.total",
            expectedValue: 1L,
            expectedTags: new Dictionary<string, object?>
            {
                ["command"] = "create",
                ["status"] = "success",
            });

        Assert.That(measurements, Is.Not.Empty);
        Assert.That(result, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that CLI commands handle multiple operations with proper telemetry.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CliCommands_MultipleOperations_ShouldCreateSeparateActivities()
    {
        // Arrange
        var serializerProvider = new Mock<IJsonSerializerOptionsProvider>();
        var getCommand = new Get(this.mockTenantProvider!.Object, serializerProvider.Object, this.mockGetLogger!.Object);
        var createCommand = new Create(this.mockTenantStore!.Object, this.mockCreateLogger!.Object);

        var getSettings = new GetSettings { TenantId = "multi-op-tenant-1" };
        var createSettings = new CreateSettings
        {
            TenantId = "multi-op-parent",
            Name = "Multi-Op Child",
            WellKnownTenantGuid = Guid.NewGuid().ToString(),
        };
        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "command", null);

        // Act
        int getResult = await getCommand.ExecuteAsync(context, getSettings);
        int createResult = await createCommand.ExecuteAsync(context, createSettings);

        // Assert
        await this.telemetryScope!.WaitForActivitiesAsync(2);

        List<Activity> activities = this.telemetryScope.CapturedActivities;
        Assert.That(activities.Count, Is.EqualTo(2));

        // Verify both activities are present with correct names
        Assert.That(activities.Exists(a => a.DisplayName == "cli.get-tenant"), Is.True);
        Assert.That(activities.Exists(a => a.DisplayName == "cli.create-tenant"), Is.True);

        Assert.That(getResult, Is.EqualTo(0));
        Assert.That(createResult, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that CLI commands properly tag activities with operation details.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CliCommands_ShouldTagActivitiesWithOperationDetails()
    {
        // Arrange
        var serializerProvider = new Mock<IJsonSerializerOptionsProvider>();
        var listCommand = new Marain.Tenancy.Cli.Commands.List(this.mockTenantStore!.Object, serializerProvider.Object, this.mockListLogger!.Object);
        var settings = new ListSettings
        {
            TenantId = "detailed-parent-tenant",
            Name = true,
        };
        var context = new CommandContext([], Mock.Of<IRemainingArguments>(), "list", null);

        // Mock children response with multiple levels
        TenantCollectionResult tenantCollectionResult = new(["child1", "child2"], null);
        this.mockTenantStore!.Setup(x => x.GetChildrenAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>())).Returns(Task.FromResult(tenantCollectionResult));

        // Act
        int result = await listCommand.ExecuteAsync(context, settings);

        // Assert
        await this.telemetryScope!.WaitForActivitiesAsync(1);

        Activity activity = this.telemetryScope.ValidateActivity(
            "cli.list-tenants",
            expectedTags: new Dictionary<string, object?>
            {
                [TelemetryConstants.AttributeKeys.ParentTenantId] = "detailed-parent-tenant",
                [TelemetryConstants.AttributeKeys.CommandType] = TelemetryConstants.OperationTypes.List,
                ["name"] = "true",
            },
            expectedStatus: ActivityStatusCode.Ok);

        Assert.That(activity, Is.Not.Null);
        Assert.That(result, Is.EqualTo(0));
    }
}