// <copyright file="MinimalApiTestableTenancyService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.MultiHost;

using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;

/// <summary>
/// Implementation of ITestableTenancyService that uses WebApplicationFactory to test the MinimalApi.
/// </summary>
internal class MinimalApiTestableTenancyService : ITestableTenancyService, IDisposable
{
    private readonly WebApplicationFactory<MinimalApiTestableTenancyService> factory;
    private readonly HttpClient httpClient;
    private HttpResponseMessage? response;
    private string? responseContent;
    private JObject? parsedResponse;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinimalApiTestableTenancyService"/> class.
    /// </summary>
    public MinimalApiTestableTenancyService()
    {
        this.factory = new MinimalApiWebApplicationFactory();
        this.httpClient = this.factory.CreateClient();
    }

    /// <inheritdoc/>
    public async Task<TenancyResponse> CreateTenantAsync(string parentId, string name)
    {
        var requestBody = new { TenantName = name };
        await this.SendPostRequest($"/{parentId}/marain/tenant", requestBody);
        return this.MakeResponse();
    }

    /// <inheritdoc/>
    public async Task<TenancyResponse> GetSwaggerAsync()
    {
        await this.SendGetRequest("/swagger/v1/swagger.json");
        return this.MakeResponse();
    }

    /// <inheritdoc/>
    public async Task<TenancyResponse> GetTenantAsync(string tenantId, string? etag)
    {
        await this.SendGetRequest($"/{tenantId}/marain/tenant", etag);
        return this.MakeResponse();
    }

    /// <inheritdoc/>
    public async Task<TenancyResponse> GetTenantByLocationAsync(string location)
    {
        await this.SendGetRequest(location);
        return this.MakeResponse();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.httpClient?.Dispose();
        this.response?.Dispose();
    }

    private TenancyResponse MakeResponse()
    {
        return new TenancyResponse
        {
            LocationHeader = this.response!.Headers.Location?.ToString(),
            EtagHeader = this.response.Headers.ETag?.Tag,
            IsSuccessStatusCode = this.response.IsSuccessStatusCode,
            StatusCode = this.response.StatusCode,
            CacheControlHeader = this.response.Headers.CacheControl?.ToString(),
            BodyJson = this.parsedResponse,
        };
    }

    private async Task SendGetRequest(string path, string? etag = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (!string.IsNullOrEmpty(etag))
        {
            request.Headers.Add("If-None-Match", etag);
        }

        this.response = await this.httpClient.SendAsync(request);
        this.responseContent = await this.response.Content.ReadAsStringAsync();

        if (this.response.IsSuccessStatusCode && !string.IsNullOrEmpty(this.responseContent))
        {
            try
            {
                this.parsedResponse = JObject.Parse(this.responseContent);
            }
            catch
            {
                // If JSON parsing fails, leave parsedResponse as null
                this.parsedResponse = null;
            }
        }
    }

    private async Task SendPostRequest(string path, object? data)
    {
        HttpContent? content = null;

        if (data is not null)
        {
            string requestJson = System.Text.Json.JsonSerializer.Serialize(data);
            content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
        }

        this.response = await this.httpClient.PostAsync(path, content);
        this.responseContent = await this.response.Content.ReadAsStringAsync();

        if (this.response.IsSuccessStatusCode && !string.IsNullOrEmpty(this.responseContent))
        {
            try
            {
                this.parsedResponse = JObject.Parse(this.responseContent);
            }
            catch
            {
                // If JSON parsing fails, leave parsedResponse as null
                this.parsedResponse = null;
            }
        }
    }
}

/// <summary>
/// Custom WebApplicationFactory that can create the MinimalApi application for testing.
/// </summary>
internal class MinimalApiWebApplicationFactory : WebApplicationFactory<MinimalApiTestableTenancyService>
{
    /// <inheritdoc/>
    protected override IHostBuilder CreateHostBuilder()
    {
        // Get the MinimalApi assembly
        Assembly minimalApiAssembly = Assembly.Load("Marain.Tenancy.MinimalApi");
        
        return Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<MinimalApiTestStartup>();
                webBuilder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("local.settings.json", optional: true);
                    config.AddEnvironmentVariables();
                });
                webBuilder.UseContentRoot(GetContentRoot(minimalApiAssembly));
            });
    }

    private static string GetContentRoot(Assembly assembly)
    {
        // Find the content root by looking for the MinimalApi project directory
        string assemblyLocation = assembly.Location;
        DirectoryInfo? directory = new FileInfo(assemblyLocation).Directory;
        
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Program.cs")))
        {
            directory = directory.Parent;
        }
        
        return directory?.FullName ?? Environment.CurrentDirectory;
    }
}

/// <summary>
/// Test startup class that mimics the MinimalApi Program.cs configuration.
/// </summary>
internal class MinimalApiTestStartup
{
    private readonly IConfiguration configuration;

    public MinimalApiTestStartup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // Add services similar to Program.cs
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHealthChecks();
        
        // Add tenancy minimal API services (this would need to be implemented)
        // services.AddTenancyMinimalApi();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health");
            // Map tenancy endpoints (this would need to be implemented)
            // endpoints.MapTenancyEndpoints();
        });
    }
}