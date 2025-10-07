// <copyright file="AzuriteTestFixture.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;

using System;
using System.Threading.Tasks;
using Testcontainers.Azurite;

/// <summary>
/// Test fixture that provides Azurite container for Azure Storage testing.
/// </summary>
/// <remarks>
/// This fixture manages the lifecycle of an Azurite container, providing isolated
/// Azure Storage emulation for each test class. The container is started before
/// all tests in the class and disposed after all tests complete.
/// </remarks>
public class AzuriteTestFixture : IDisposable
{
    private readonly AzuriteContainer azuriteContainer;
    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzuriteTestFixture"/> class.
    /// </summary>
    public AzuriteTestFixture()
    {
        this.azuriteContainer = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            .Build();
    }

    /// <summary>
    /// Gets the connection string for the Azurite container.
    /// </summary>
    /// <remarks>
    /// This connection string is only available after <see cref="StartAsync"/> has been called.
    /// </remarks>
    public string ConnectionString => this.azuriteContainer.GetConnectionString();

    /// <summary>
    /// Gets a value indicating whether the Azurite container is running.
    /// </summary>
    public bool IsRunning => this.azuriteContainer.State == DotNet.Testcontainers.Containers.TestcontainersStates.Running;

    /// <summary>
    /// Starts the Azurite container asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync()
    {
        if (!this.IsRunning)
        {
            await this.azuriteContainer.StartAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Stops the Azurite container asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StopAsync()
    {
        if (this.IsRunning)
        {
            await this.azuriteContainer.StopAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Disposes the Azurite container.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the Azurite container.
    /// </summary>
    /// <param name="disposing">True if disposing, false if finalizing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed && disposing)
        {
            this.azuriteContainer?.DisposeAsync().AsTask().GetAwaiter().GetResult();
            this.disposed = true;
        }
    }
}