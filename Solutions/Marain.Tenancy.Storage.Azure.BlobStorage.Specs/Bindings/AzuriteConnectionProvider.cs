// <copyright file="AzuriteConnectionProvider.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Corvus.Storage.Azure.BlobStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides Azure Storage connection strings for test scenarios with intelligent source detection.
/// </summary>
/// <remarks>
/// This provider implements a priority-based configuration system:
/// 1. Testcontainers Azurite (if available from test context)
/// 2. Environment variables
/// 3. Local settings file (local.settings.json)
/// 4. Development storage fallback.
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
    /// Gets the Azure Storage connection string using the priority-based detection system.
    /// </summary>
    /// <returns>A connection string for Azure Storage (Azurite or development storage).</returns>
    public static string GetConnectionString()
    {
        Console.WriteLine("🔍 Detecting Azure Storage connection string...");

        // Priority 1: Testcontainers (dynamic per-test allocation) - only if Docker is available
        bool dockerAvailable = IsDockerAvailable();
        bool testcontainersAvailable = IsTestcontainersAvailable;
        Console.WriteLine($"   Docker available: {dockerAvailable}, Testcontainers available: {testcontainersAvailable}");

        if (dockerAvailable && testcontainersAvailable)
        {
            Console.WriteLine("🐳 Using Testcontainers Azurite with dynamic port allocation");
            return TestcontainersConnectionString.Value!;
        }

        // Priority 2: Environment variables
        string? envConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        Console.WriteLine($"   Environment connection string: {(string.IsNullOrEmpty(envConnectionString) ? "Not set" : "Found")}");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            Console.WriteLine("🌍 Using Azure Storage connection string from environment variables");
            return envConnectionString;
        }

        // Priority 3: DevContainer service (if running in devcontainer and azurite is accessible)
        bool isDevContainer = IsDevContainerEnvironment();
        bool azuriteServiceAvailable = IsAzuriteServiceAvailable();
        Console.WriteLine($"   DevContainer environment: {isDevContainer}, Azurite service available: {azuriteServiceAvailable}");

        if (isDevContainer && azuriteServiceAvailable)
        {
            Console.WriteLine("📦 Using DevContainer Azurite service on standard ports");
            return "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;";
        }

        // Priority 4: Check if development storage emulator is available
        bool storageEmulatorAvailable = IsStorageEmulatorAvailable();
        Console.WriteLine($"   Storage emulator available: {storageEmulatorAvailable}");

        if (storageEmulatorAvailable)
        {
            Console.WriteLine("💾 Using development storage emulator");
            return "UseDevelopmentStorage=true";
        }

        // Priority 5: Last resort - in-memory storage (if available) or mock connection string
        Console.WriteLine("⚠️  No storage emulator available - using mock connection string for testing");
        Console.WriteLine("   Note: Tests may fail if they require real blob storage operations");

        // Return a connection string that won't try to connect to anything
        // This is a last resort to prevent hard failures
        return "DefaultEndpointsProtocol=https;AccountName=mockstorage;AccountKey=bW9ja2tleQ==;EndpointSuffix=core.windows.net";
    }

    /// <summary>
    /// Gets a BlobContainerConfiguration using the intelligent connection string detection.
    /// </summary>
    /// <returns>A BlobContainerConfiguration for the detected storage.</returns>
    public static BlobContainerConfiguration GetBlobContainerConfiguration()
    {
        string connectionString = GetConnectionString();

        return new BlobContainerConfiguration
        {
            ConnectionStringPlainText = connectionString,
        };
    }

    /// <summary>
    /// Creates an enhanced IConfiguration that includes dynamic Azurite connection strings.
    /// </summary>
    /// <param name="baseConfiguration">The base configuration to enhance.</param>
    /// <returns>An enhanced configuration with Azurite connection string.</returns>
    public static IConfiguration CreateEnhancedConfiguration(IConfiguration? baseConfiguration = null)
    {
        var configBuilder = new ConfigurationBuilder();

        // Get the intelligent connection string
        string intelligentConnectionString = GetConnectionString();

        // Add dynamic connection string with highest priority
        var dynamicConfig = new Dictionary<string, string?>
        {
            ["RootBlobStorageConfiguration:ConnectionStringPlainText"] = intelligentConnectionString,
        };
        configBuilder.AddInMemoryCollection(dynamicConfig);

        // Add environment variables
        configBuilder.AddEnvironmentVariables();

        // Add base configuration if provided
        if (baseConfiguration != null)
        {
            configBuilder.AddConfiguration(baseConfiguration);
        }

        // Only add local.settings.json if we're NOT using the DevContainer service or testcontainers
        // This prevents local.settings.json from overriding our intelligent detection
        bool isUsingIntelligentDetection = intelligentConnectionString.Contains("azurite:10000") ||
                                         (!string.IsNullOrEmpty(TestcontainersConnectionString.Value));

        if (!isUsingIntelligentDetection)
        {
            Console.WriteLine("   Adding local.settings.json to configuration");
            configBuilder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        }
        else
        {
            Console.WriteLine("   ⚠️  Bypassing local.settings.json - using intelligent connection detection");
        }

        return configBuilder.Build();
    }

    /// <summary>
    /// Detects if Docker is available and running on this system.
    /// </summary>
    /// <returns>True if Docker is available, false otherwise.</returns>
    public static bool IsDockerAvailable()
    {
        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "info",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };

            process.Start();
            bool finished = process.WaitForExit(5000); // 5 second timeout

            if (!finished)
            {
                try
                {
                    process.Kill();
                }
                catch (InvalidOperationException)
                {
                    // Process may have already exited
                }

                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is FileNotFoundException)
        {
            // Docker executable not found or not accessible
            return false;
        }
        catch (Exception)
        {
            // Other unexpected errors
            return false;
        }
    }

    /// <summary>
    /// Detects if the Azure Storage Emulator (development storage) is available.
    /// </summary>
    /// <returns>True if storage emulator is reachable, false otherwise.</returns>
    private static bool IsStorageEmulatorAvailable()
    {
        try
        {
            using var client = new TcpClient();
            System.Threading.Tasks.Task connectTask = client.ConnectAsync("127.0.0.1", 10000);
            bool connected = connectTask.Wait(1000); // 1 second timeout
            return connected && client.Connected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Debug: Storage emulator check failed: {ex.GetType().Name} - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Detects if the Azurite service is available on the expected hostname.
    /// </summary>
    /// <returns>True if Azurite service is reachable, false otherwise.</returns>
    private static bool IsAzuriteServiceAvailable()
    {
        try
        {
            using var client = new TcpClient();
            System.Threading.Tasks.Task connectTask = client.ConnectAsync("azurite", 10000);
            bool connected = connectTask.Wait(2000); // 2 second timeout
            return connected && client.Connected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Debug: Azurite service check failed: {ex.GetType().Name} - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Detects if the code is running in a DevContainer environment.
    /// </summary>
    /// <returns>True if running in a DevContainer, false otherwise.</returns>
    private static bool IsDevContainerEnvironment()
    {
        // Check for common DevContainer environment indicators
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REMOTE_CONTAINERS")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CODESPACES")) ||
               Environment.GetEnvironmentVariable("TERM_PROGRAM") == "vscode";
    }
}