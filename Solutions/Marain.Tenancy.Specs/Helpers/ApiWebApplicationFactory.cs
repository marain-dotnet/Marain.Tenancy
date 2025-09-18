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
internal class ApiWebApplicationFactory : WebApplicationFactory<Api.Program>, IDisposable
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