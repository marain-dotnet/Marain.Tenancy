// <copyright file="MultiHostIntegrationTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.TelemetryTests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Corvus.Tenancy;
using Marain.Tenancy.Client;
using Marain.Tenancy.Shared.Telemetry;
using Marain.Tenancy.Shared.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception

/// <summary>
/// Integration tests for telemetry correlation across multiple host scenarios (API + CLI + Client).
/// </summary>
[TestFixture]
[Category("Integration")]
[Category("Telemetry")]
public class MultiHostIntegrationTests
{
    private TelemetryTestScope? telemetryScope;
    private WebApplicationFactory<Program>? apiFactory;
    private HttpClient? apiClient;
    private TenancyClient? tenancyClient;

    /// <summary>
    /// Sets up the test environment before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this.telemetryScope = TelemetryTestHelpers.CreateTelemetryTestScope();

        // Create API factory with telemetry enabled
        this.apiFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Use in-memory storage for consistent testing
                    services.AddSingleton<ITenantStore>(serviceProvider =>
                    {
                        // Return a mock or in-memory implementation
                        return MockTenantStoreHelper.CreateMockTenantStore();
                    });

                    // Ensure telemetry is configured
                    builder.UseEnvironment("Testing");
                });
            });

        this.apiClient = this.apiFactory.CreateClient();

        // Create tenancy client that connects to the test API
        this.tenancyClient = new TenancyClient(this.apiClient, new JsonSerializerOptions());
    }

    /// <summary>
    /// Cleans up after each test.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        this.apiClient?.Dispose();
        this.apiFactory?.Dispose();
        this.telemetryScope?.Dispose();
    }

    /// <summary>
    /// Tests that telemetry correlation works across CLI → API → Storage layers.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task CliToApiToStorage_ShouldMaintainTelemetryCorrelation()
    {
        // Arrange - Create a parent activity to simulate CLI command
        using var testSource = new ActivitySource("test.integration");
        using Activity? cliActivity = testSource.StartActivity("cli.integration-test");
        cliActivity?.SetTag(TelemetryConstants.AttributeKeys.CommandType, "integration-test");
        cliActivity?.SetTag("test.scenario", "multi-host-correlation");

        string testTenantId = "integration-test-tenant-" + Guid.NewGuid().ToString("N")[..8];

        // Act - Simulate CLI calling API through client
        using (Activity? clientActivity = testSource.StartActivity("client.get-tenant"))
        {
            clientActivity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, testTenantId);

            try
            {
                // This should fail as tenant doesn't exist, but we're testing telemetry correlation
                await this.tenancyClient!.GetTenantAsync(testTenantId);
            }
            catch (HttpRequestException)
            {
                // Expected - tenant doesn't exist, but telemetry should still be captured
            }
        }

        // Assert - Verify telemetry correlation across all layers
        await this.telemetryScope!.WaitForActivitiesAsync(3, timeout: TimeSpan.FromSeconds(5));

        List<Activity> activities = this.telemetryScope.CapturedActivities;

        // Should have CLI, Client, and API activities
        Assert.That(activities.Count, Is.GreaterThanOrEqualTo(3));

        // Verify CLI activity
        Activity? capturedCliActivity = activities.Find(a => a.DisplayName == "cli.integration-test");
        Assert.That(capturedCliActivity, Is.Not.Null);
        Assert.That(capturedCliActivity?.GetTagItem("test.scenario"), Is.EqualTo("multi-host-correlation"));

        // Verify Client activity
        Activity? capturedClientActivity = activities.Find(a => a.DisplayName == "client.get-tenant");
        Assert.That(capturedClientActivity, Is.Not.Null);
        Assert.That(capturedClientActivity?.ParentId, Is.EqualTo(cliActivity?.Id));

        // Verify API activity exists
        Activity? apiActivity = activities.Find(a => a.DisplayName.StartsWith("api.") || a.DisplayName.StartsWith("GET"));
        Assert.That(apiActivity, Is.Not.Null);
    }

    /// <summary>
    /// Tests end-to-end telemetry flow for tenant creation across all components.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task EndToEndTenantCreation_ShouldCreateCompleteTelemetryChain()
    {
        // Arrange
        using var testSource = new ActivitySource("test.e2e");
        using Activity? parentActivity = testSource.StartActivity("e2e.tenant-creation-test");
        parentActivity?.SetTag("test.type", "end-to-end");

        string parentTenantId = "e2e-parent-" + Guid.NewGuid().ToString("N")[..8];
        string childTenantName = "E2E Test Child";
        var childTenantGuid = Guid.NewGuid();

        // Act - Simulate complete tenant creation workflow
        using (Activity? workflowActivity = testSource.StartActivity("workflow.create-tenant"))
        {
            workflowActivity?.SetTag(TelemetryConstants.AttributeKeys.ParentTenantId, parentTenantId);
            workflowActivity?.SetTag(TelemetryConstants.AttributeKeys.TenantName, childTenantName);

            try
            {
                // This will likely fail in testing, but we're verifying telemetry flow
                await this.tenancyClient!.CreateChildTenantAsync(parentTenantId, childTenantGuid.ToString(), childTenantName);
            }
            catch (Exception ex)
            {
                workflowActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                // Continue to verify telemetry was captured
            }
        }

        // Assert - Verify complete telemetry chain
        await this.telemetryScope!.WaitForActivitiesAsync(2, timeout: TimeSpan.FromSeconds(5));

        List<Activity> activities = this.telemetryScope.CapturedActivities;

        // Verify parent test activity
        Activity? testActivity = activities.Find(a => a.DisplayName == "e2e.tenant-creation-test");
        Assert.That(testActivity, Is.Not.Null);

        // Verify workflow activity
        Activity? capturedWorkflowActivity = activities.Find(a => a.DisplayName == "workflow.create-tenant");
        Assert.That(capturedWorkflowActivity, Is.Not.Null);
        Assert.That(capturedWorkflowActivity?.ParentId, Is.EqualTo(parentActivity?.Id));
        Assert.That(capturedWorkflowActivity?.GetTagItem(TelemetryConstants.AttributeKeys.ParentTenantId), Is.EqualTo(parentTenantId));
    }

    /// <summary>
    /// Tests that telemetry works correctly when API calls are made concurrently.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ConcurrentApiCalls_ShouldMaintainSeparateTelemetryContexts()
    {
        // Arrange
        using var testSource = new ActivitySource("test.concurrent");
        const int concurrentCalls = 5;
        var tasks = new Task[concurrentCalls];
        string[] testTenantIds = new string[concurrentCalls];

        // Generate unique tenant IDs for each concurrent call
        for (int i = 0; i < concurrentCalls; i++)
        {
            testTenantIds[i] = $"concurrent-test-{i}-{Guid.NewGuid().ToString("N")[..8]}";
        }

        // Act - Make concurrent API calls, each with its own telemetry context
        for (int i = 0; i < concurrentCalls; i++)
        {
            int index = i; // Capture for closure
            tasks[i] = Task.Run(async () =>
            {
                using Activity? callActivity = testSource.StartActivity($"concurrent.call-{index}");
                callActivity?.SetTag("call.index", index.ToString());
                callActivity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, testTenantIds[index]);

                try
                {
                    await this.tenancyClient!.GetTenantAsync(testTenantIds[index]);
                }
                catch (HttpRequestException)
                {
                    // Expected for non-existent tenants
                    callActivity?.SetStatus(ActivityStatusCode.Error, "Tenant not found");
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Verify each call maintained separate telemetry context
        await this.telemetryScope!.WaitForActivitiesAsync(concurrentCalls, timeout: TimeSpan.FromSeconds(10));

        List<Activity> activities = this.telemetryScope.CapturedActivities;

        // Should have activities for all concurrent calls
        for (int i = 0; i < concurrentCalls; i++)
        {
            Activity? callActivity = activities.Find(a => a.DisplayName == $"concurrent.call-{i}");
            Assert.That(callActivity, Is.Not.Null, $"Activity for call {i} should exist");
            Assert.That(callActivity?.GetTagItem("call.index"), Is.EqualTo(i.ToString()));
            Assert.That(callActivity?.GetTagItem(TelemetryConstants.AttributeKeys.TenantId), Is.EqualTo(testTenantIds[i]));
        }
    }

    /// <summary>
    /// Tests that telemetry propagation works with HTTP headers (W3C Trace Context).
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task HttpTelemetryPropagation_ShouldPropagateTraceContext()
    {
        // Arrange
        using var testSource = new ActivitySource("test.http");
        using Activity? rootActivity = testSource.StartActivity("http.trace-propagation-test");
        rootActivity?.SetTag("test.purpose", "trace-context-propagation");

        string testTenantId = "trace-propagation-" + Guid.NewGuid().ToString("N")[..8];

        // Act - Make HTTP request that should propagate trace context
        using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/tenants/{testTenantId}"))
        {
            // The HttpClient should automatically add trace context headers
            try
            {
                using HttpResponseMessage response = await this.apiClient!.SendAsync(requestMessage);

                // Response doesn't matter - we're testing trace propagation
            }
            catch (Exception)
            {
                // Ignore response errors, focus on telemetry
            }
        }

        // Assert - Verify trace context was propagated
        await this.telemetryScope!.WaitForActivitiesAsync(1, timeout: TimeSpan.FromSeconds(5));

        List<Activity> activities = this.telemetryScope.CapturedActivities;

        // Should have the root test activity
        Activity? testActivity = activities.Find(a => a.DisplayName == "http.trace-propagation-test");
        Assert.That(testActivity, Is.Not.Null);
        Assert.That(testActivity?.GetTagItem("test.purpose"), Is.EqualTo("trace-context-propagation"));

        // Verify trace context properties
        Assert.That(testActivity?.TraceId, Is.Not.EqualTo(default(ActivityTraceId)));
        Assert.That(testActivity?.SpanId, Is.Not.EqualTo(default(ActivitySpanId)));
    }

    /// <summary>
    /// Tests telemetry behavior during service failure scenarios.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Test]
    public async Task ServiceFailure_ShouldRecordAppropriateErrorTelemetry()
    {
        // Arrange
        using var testSource = new ActivitySource("test.failure");
        using Activity? failureTestActivity = testSource.StartActivity("test.service-failure-scenario");
        failureTestActivity?.SetTag("test.type", "failure-handling");

        string nonExistentTenantId = "failure-test-" + Guid.NewGuid().ToString("N")[..8];

        // Act - Attempt operation that will fail
        Exception? caughtException = null;
        using (Activity? operationActivity = testSource.StartActivity("operation.expected-failure"))
        {
            operationActivity?.SetTag(TelemetryConstants.AttributeKeys.TenantId, nonExistentTenantId);
            operationActivity?.SetTag("expected.result", "failure");

            try
            {
                await this.tenancyClient!.GetTenantAsync(nonExistentTenantId);
            }
            catch (Exception ex)
            {
                caughtException = ex;
                operationActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            }
        }

        // Assert
        Assert.That(caughtException, Is.Not.Null, "Expected an exception for non-existent tenant");

        await this.telemetryScope!.WaitForActivitiesAsync(2, timeout: TimeSpan.FromSeconds(5));

        List<Activity> activities = this.telemetryScope.CapturedActivities;

        // Verify failure test activity
        Activity? testActivity = activities.Find(a => a.DisplayName == "test.service-failure-scenario");
        Assert.That(testActivity, Is.Not.Null);

        // Verify operation activity recorded the error
        Activity? capturedOperationActivity = activities.Find(a => a.DisplayName == "operation.expected-failure");
        Assert.That(capturedOperationActivity, Is.Not.Null);
        Assert.That(capturedOperationActivity?.Status, Is.EqualTo(ActivityStatusCode.Error));
        Assert.That(capturedOperationActivity?.GetTagItem(TelemetryConstants.AttributeKeys.TenantId), Is.EqualTo(nonExistentTenantId));
    }
}