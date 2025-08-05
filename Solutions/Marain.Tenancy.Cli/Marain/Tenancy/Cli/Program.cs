// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Cli;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

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
            services.AddJsonNetSerializerSettingsProvider();
            services.AddJsonNetPropertyBag();
            services.AddJsonNetCultureInfoConverter();
            services.AddJsonNetDateTimeOffsetToIso8601AndUnixTimeConverter();
            services.AddSingleton<JsonConverter>(new StringEnumConverter(new CamelCaseNamingStrategy()));

            string tenancyServiceBaseUri = ctx.Configuration["TenancyClient:TenancyServiceBaseUri"]
                ?? throw new InvalidOperationException("TenancyClient:TenancyServiceBaseUri configuration is required");

            services.AddTenantProviderServiceClient(tenancyServiceBaseUri);
        });

        await builder.RunCommandLineApplicationAsync<TenancyCliCommand>(args).ConfigureAwait(false);
    }
}