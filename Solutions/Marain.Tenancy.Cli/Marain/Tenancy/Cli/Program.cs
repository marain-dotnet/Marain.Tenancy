// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli;

using System;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        });

        await builder.RunCommandLineApplicationAsync<TenancyCliCommand>(args).ConfigureAwait(false);
    }
}