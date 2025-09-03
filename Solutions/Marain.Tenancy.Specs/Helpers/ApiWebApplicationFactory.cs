// <copyright file="ApiWebApplicationFactory.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Helpers;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Testcontainers.Azurite;

/// <summary>
/// Custom WebApplicationFactory that can create the API application for testing.
/// </summary>
internal class ApiWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private AzuriteContainer azuriteContainer;
    private HttpClient? client;

    private static ApiWebApplicationFactory? current;

    private ApiWebApplicationFactory()
    {
        // Set up Testcontainer
        this.azuriteContainer = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            .Build();
    }

    public static ApiWebApplicationFactory Current
    {
        get
        {
            current ??= new ApiWebApplicationFactory();
            return current;
        }
    }

    public HttpClient Client
    {
        get
        {
            this.client ??= this.CreateClient();
            return this.client;
        }
    }

    public override ValueTask DisposeAsync()
    {
        return this.azuriteContainer.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Start the Azurite container.
        // Nasty async-in-a-sync-method here, because all these startup methods are async.
        this.azuriteContainer.StartAsync().GetAwaiter().GetResult();

        string connectionString = this.azuriteContainer.GetConnectionString();

        Environment.SetEnvironmentVariable("RootBlobStorageConfiguration:ConnectionStringPlainText", connectionString);

        builder.UseEnvironment(Environments.Development);
    }

    /////// <inheritdoc/>
    ////protected override IHostBuilder CreateHostBuilder()
    ////{
    ////    // Get the MinimalApi assembly
    ////    var apiAssembly = Assembly.Load("Marain.Tenancy.Api");

    ////    return Host.CreateDefaultBuilder()
    ////        .ConfigureWebHostDefaults(webBuilder =>
    ////        {
    ////            webBuilder.UseStartup<ApiTestStartup>();
    ////            webBuilder.ConfigureAppConfiguration((context, config) =>
    ////            {
    ////                config.AddJsonFile("local.settings.json", optional: true);
    ////                config.AddEnvironmentVariables();
    ////            });
    ////            webBuilder.UseContentRoot(GetContentRoot(minimalApiAssembly));
    ////        });
    ////}

    ////private static string GetContentRoot(Assembly assembly)
    ////{
    ////    // Find the content root by looking for the Api project directory
    ////    string assemblyLocation = assembly.Location;
    ////    DirectoryInfo? directory = new FileInfo(assemblyLocation).Directory;

    ////    while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Program.cs")))
    ////    {
    ////        directory = directory.Parent;
    ////    }

    ////    return directory?.FullName ?? Environment.CurrentDirectory;
    ////}
}