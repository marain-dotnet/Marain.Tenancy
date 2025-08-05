// <copyright file="AzuriteTestBase.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;

using System;
using System.Threading.Tasks;
using NUnit.Framework;

/// <summary>
/// Base class for tests that require Azurite container.
/// </summary>
/// <remarks>
/// This base class provides automatic Azurite container lifecycle management
/// for test classes. Derive from this class to get access to isolated Azure
/// Storage emulation via the <see cref="AzuriteFixture"/> property.
/// </remarks>
public abstract class AzuriteTestBase : IDisposable
{
    private readonly AzuriteTestFixture azuriteFixture;
    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzuriteTestBase"/> class.
    /// </summary>
    protected AzuriteTestBase()
    {
        this.azuriteFixture = new AzuriteTestFixture();
    }

    /// <summary>
    /// Gets the Azurite test fixture.
    /// </summary>
    protected AzuriteTestFixture AzuriteFixture => this.azuriteFixture;

    /// <summary>
    /// Sets up the Azurite container before all tests in the class.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [OneTimeSetUp]
    public async Task OneTimeSetUpAsync()
    {
        await this.azuriteFixture.StartAsync().ConfigureAwait(false);
        await this.AdditionalSetupAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Tears down the Azurite container after all tests in the class.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [OneTimeTearDown]
    public async Task OneTimeTearDownAsync()
    {
        await this.AdditionalTeardownAsync().ConfigureAwait(false);
        await this.azuriteFixture.StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the test base and its resources.
    /// </summary>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs additional setup after Azurite container starts.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Override this method to perform additional setup that depends on
    /// the Azurite container being available.
    /// </remarks>
    protected virtual Task AdditionalSetupAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs additional teardown before Azurite container stops.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Override this method to perform cleanup before the Azurite
    /// container is stopped.
    /// </remarks>
    protected virtual Task AdditionalTeardownAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the test base and its resources.
    /// </summary>
    /// <param name="disposing">True if disposing, false if finalizing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed && disposing)
        {
            this.azuriteFixture?.Dispose();
            this.disposed = true;
        }
    }
}