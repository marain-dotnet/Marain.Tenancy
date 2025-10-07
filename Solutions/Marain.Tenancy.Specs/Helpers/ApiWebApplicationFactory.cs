// <copyright file="ApiWebApplicationFactory.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Helpers;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.Azurite;

/// <summary>
/// Custom WebApplicationFactory that can create the API application for testing.
/// </summary>
internal sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly AzuriteContainer azuriteContainer;
    private HttpClient? client;

    private static ApiWebApplicationFactory? current;

    private ApiWebApplicationFactory()
    {
        // Disable ResourceReaper to avoid timeout issues in container environments
        Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");

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

    public override async ValueTask DisposeAsync()
    {
        this.client?.Dispose();

        await this.azuriteContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Start the Azurite container.
        // Nasty async-in-a-sync-method here, because all these startup methods are async.
        this.azuriteContainer.StartAsync().GetAwaiter().GetResult();

        string connectionString = this.azuriteContainer.GetConnectionString();

        Console.WriteLine($"Azurite connection string: {connectionString}");
        Environment.SetEnvironmentVariable("RootBlobStorageConfiguration:ConnectionStringPlainText", connectionString);

        // Set minimal Azure AD configuration to prevent validation errors
        Environment.SetEnvironmentVariable("AzureAd:Instance", "https://login.microsoftonline.com/");
        Environment.SetEnvironmentVariable("AzureAd:Domain", "example.com");
        Environment.SetEnvironmentVariable("AzureAd:TenantId", "00000000-0000-0000-0000-000000000000");
        Environment.SetEnvironmentVariable("AzureAd:ClientId", "00000000-0000-0000-0000-000000000000");
        Environment.SetEnvironmentVariable("AzureAd:AllowWebApiToBeAuthorizedByACL", "true");

        builder.UseEnvironment(Environments.Development);

        // Configure test services to bypass JWT validation. This means the API will accept any token that has the correct structure.
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    RequireSignedTokens = false,
                    SignatureValidator = (token, parameters) => new JsonWebToken(token), // Accept any token without signature validation
                };
            });
        });
    }
}