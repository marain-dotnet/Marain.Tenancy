// <copyright file="AzuriteConnectionProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;

using System;
using System.Threading;
using Corvus.Storage.Azure.BlobStorage;

/// <summary>
/// Provides Azure Storage connection strings for test scenarios using Testcontainers.Net.
/// </summary>
/// <remarks>
/// This provider manages connection strings from Testcontainers Azurite containers.
/// Connection strings must be set via SetTestcontainersConnectionString() before use.
/// </remarks>
public static class AzuriteConnectionProvider
{
    private static readonly ThreadLocal<string?> TestcontainersConnectionString = new ThreadLocal<string?>();

    /// <summary>
    /// Gets a value indicating whether a Testcontainers connection string is available.
    /// </summary>
    public static bool IsTestcontainersAvailable => !string.IsNullOrEmpty(TestcontainersConnectionString.Value);

    /// <summary>
    /// Sets the Testcontainers connection string for the current test thread.
    /// </summary>
    /// <param name="connectionString">The connection string from Testcontainers Azurite.</param>
    /// <remarks>
    /// This should be called from test fixtures or setup methods to provide
    /// the dynamically allocated Azurite connection string.
    /// </remarks>
    public static void SetTestcontainersConnectionString(string connectionString)
    {
        TestcontainersConnectionString.Value = connectionString;
    }

    /// <summary>
    /// Clears the Testcontainers connection string for the current test thread.
    /// </summary>
    /// <remarks>
    /// This should be called from test teardown methods to clean up
    /// thread-local storage.
    /// </remarks>
    public static void ClearTestcontainersConnectionString()
    {
        TestcontainersConnectionString.Value = null;
    }

    /// <summary>
    /// Gets the Azure Storage connection string from Testcontainers.
    /// </summary>
    /// <returns>A connection string for Testcontainers Azurite.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no Testcontainers connection string is available.</exception>
    public static string GetConnectionString()
    {
        if (string.IsNullOrEmpty(TestcontainersConnectionString.Value))
        {
            throw new InvalidOperationException(
                "No Testcontainers connection string available. " +
                "Ensure SetTestcontainersConnectionString() is called before using this provider.");
        }

        return TestcontainersConnectionString.Value;
    }

    /// <summary>
    /// Gets a BlobContainerConfiguration using the Testcontainers connection string.
    /// </summary>
    /// <returns>A BlobContainerConfiguration for Testcontainers Azurite.</returns>
    public static BlobContainerConfiguration GetBlobContainerConfiguration()
    {
        string connectionString = GetConnectionString();

        return new BlobContainerConfiguration
        {
            ConnectionStringPlainText = connectionString,
        };
    }

}