// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli;

using System;
using System.Threading.Tasks;
using Azure.Identity;
using Marain.Tenancy.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;

/// <summary>
/// The entry point for the application. Configures the commands.
/// </summary>
public static class Program
{
    /// <summary>
    /// The entry point method.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        IHostBuilder builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices((ctx, services) =>
        {
            services.AddJsonSerializerOptionsProvider((_, options) => options.WriteIndented = true);

            services.AddJsonCultureInfoConverter();
            services.AddJsonDateTimeOffsetToIso8601AndUnixTimeConverter();
            services.AddCamelCaseConverterForEnums();
            services.AddJsonPropertyBagFactory();

            string tenancyServiceBaseUri = ctx.Configuration["TenancyClient:TenancyServiceBaseUri"]
                ?? throw new InvalidOperationException("TenancyClient:TenancyServiceBaseUri configuration is required");
            string? tenancyServiceResourceIdForMsiAuthentication = ctx.Configuration["TenancyClient:ResourceIdForMsiAuthentication"];

            string? azureServicesAuthConnectionString = ctx.Configuration["AzureServicesAuthConnectionString"];

            if (!string.IsNullOrEmpty(azureServicesAuthConnectionString))
            {
                services.AddServiceIdentityAzureTokenCredentialSourceFromLegacyConnectionString(azureServicesAuthConnectionString);
            }
            else
            {
                services.AddServiceIdentityAzureTokenCredentialSourceFromAzureCoreTokenCredential(new DefaultAzureCredential());
            }

            services.AddTenancyClient(
                sp =>
                {
                    return new()
                    {
                        BaseUri = tenancyServiceBaseUri,
                        ResourceIdForMsiAuthentication = tenancyServiceResourceIdForMsiAuthentication,
                    };
                });

            services.AddTenantProviderServiceClient();

            // Register CLI commands
            services.AddTransient<Get>();
            services.AddTransient<List>();
            services.AddTransient<Create>();
            services.AddTransient<Delete>();
        });

        IHost host = builder.Build();
        IServiceProvider services = host.Services;

        var app = new CommandApp(new TypeRegistrar(services));

        app.Configure(config =>
        {
            config.AddCommand<Get>("get")
                  .WithDescription("Gets tenant details");

            config.AddCommand<List>("list")
                  .WithDescription("List tenants");

            config.AddCommand<Create>("create")
                  .WithDescription("Create a new tenant");

            config.AddCommand<Delete>("delete")
                  .WithDescription("Deletes a tenant");
        });

        await app.RunAsync(args).ConfigureAwait(false);
    }
}