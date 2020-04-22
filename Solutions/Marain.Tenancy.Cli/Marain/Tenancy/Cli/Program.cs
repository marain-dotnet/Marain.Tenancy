// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli
{
    using System;
    using System.Threading.Tasks;
    using Corvus.Identity.ManagedServiceIdentity.ClientAuthentication;
    using Marain.Tenancy.Client;
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
                services.AddJsonSerializerSettings();

                var msiTokenSourceOptions = new AzureManagedIdentityTokenSourceOptions
                {
                    AzureServicesAuthConnectionString = ctx.Configuration["AzureServicesAuthConnectionString"],
                };

                services.AddAzureManagedIdentityBasedTokenSource(msiTokenSourceOptions);

                var tenancyClientOptions = new TenancyClientOptions
                {
                    TenancyServiceBaseUri = new Uri(ctx.Configuration["TenancyClient:TenancyServiceBaseUri"]),
                    ResourceIdForMsiAuthentication = ctx.Configuration["TenancyClient:ResourceIdForMsiAuthentication"],
                };

                services.AddSingleton(tenancyClientOptions);

                services.AddTenantProviderServiceClient();
            });

            await builder.RunCommandLineApplicationAsync<TenancyCliCommand>(args).ConfigureAwait(false);
        }
    }
}
