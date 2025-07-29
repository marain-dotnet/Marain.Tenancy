// <copyright file="AzuriteContainerBinding.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;

using System;
using System.Threading.Tasks;
using Corvus.Testing.ReqnRoll;
using Reqnroll;

/// <summary>
/// Binding that manages Azurite container lifecycle for test scenarios.
/// </summary>
/// <remarks>
/// This binding automatically starts an Azurite container before scenarios
/// tagged with @withBlobStorageTenantProvider and provides the connection
/// string to the AzuriteConnectionProvider for use by other bindings.
/// </remarks>
[Binding]
public class AzuriteContainerBinding
{
    private readonly ScenarioContext scenarioContext;
    private AzuriteTestFixture? azuriteFixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzuriteContainerBinding"/> class.
    /// </summary>
    /// <param name="scenarioContext">The scenario context from Reqnroll.</param>
    public AzuriteContainerBinding(ScenarioContext scenarioContext)
    {
        this.scenarioContext = scenarioContext;
    }

    /// <summary>
    /// Starts the Azurite container before scenarios that require blob storage.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [BeforeScenario("@withBlobStorageTenantProvider", Order = ContainerBeforeScenarioOrder.PopulateServiceCollection - 1)]
    public async Task StartAzuriteContainer()
    {
        try
        {
            this.azuriteFixture = new AzuriteTestFixture();
            await this.azuriteFixture.StartAsync().ConfigureAwait(false);

            // Provide the connection string to the connection provider
            AzuriteConnectionProvider.SetTestcontainersConnectionString(this.azuriteFixture.ConnectionString);

            // Store the fixture in scenario context for access by other bindings if needed
            this.scenarioContext.Set(this.azuriteFixture);

            Console.WriteLine($"✅ Azurite container started successfully");
            Console.WriteLine($"   Connection string: {this.azuriteFixture.ConnectionString}");
            Console.WriteLine($"   Blob endpoint: {this.azuriteFixture.BlobServiceEndpoint}");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Docker"))
        {
            Console.WriteLine("⚠️  Docker is not running or misconfigured - tests will use fallback storage");
            Console.WriteLine("   For optimal testing experience, ensure Docker is running");
            Console.WriteLine($"   Error details: {ex.Message}");

            // Don't call GetConnectionString here to avoid double-logging
            Console.WriteLine("   Tests will attempt to use DevContainer Azurite service or other fallbacks");

            // Don't set testcontainers connection string - let AzuriteConnectionProvider handle fallback
            this.azuriteFixture = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Failed to start Azurite container: {ex.GetType().Name}");
            Console.WriteLine($"   Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner error: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
            }

            Console.WriteLine("   Tests will use fallback storage configuration");

            // Don't set testcontainers connection string - let AzuriteConnectionProvider handle fallback
            this.azuriteFixture = null;
        }
    }

    /// <summary>
    /// Stops the Azurite container after scenarios complete.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [AfterScenario("@withBlobStorageTenantProvider")]
    public async Task StopAzuriteContainer()
    {
        if (this.azuriteFixture != null)
        {
            Console.WriteLine("Stopping Azurite container...");
            await this.azuriteFixture.StopAsync().ConfigureAwait(false);
            this.azuriteFixture.Dispose();
            this.azuriteFixture = null;
        }

        // Clean up the connection string from the provider
        AzuriteConnectionProvider.ClearTestcontainersConnectionString();
    }
}