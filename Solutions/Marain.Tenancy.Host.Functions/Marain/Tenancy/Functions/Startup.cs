// <copyright file="Startup.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.WebJobs.Hosting.WebJobsStartup(typeof(Marain.Tenancy.ControlHost.Startup))]

namespace Marain.Tenancy.ControlHost
{
    using System;
    using System.Linq;
    using Corvus.Azure.Storage.Tenancy;
    using Menes;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Startup code for the Function.
    /// </summary>
    public class Startup : IWebJobsStartup
    {
        /// <inheritdoc/>
        public void Configure(IWebJobsBuilder builder)
        {
            IServiceCollection services = builder.Services;

            services.AddLogging();

            // TODO: putting TelemetryClient in manually to work around regression
            // introduced in Functions v3 - see:
            // https://github.com/Azure/azure-functions-host/issues/5353
            // Apparently this has been fixed:
            // https://github.com/Azure/azure-functions-host/pull/5551
            // but at time of writing this code (11th Feb 2020) that fix was not
            // yet available for use.
            string key = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
            var tc = new TelemetryClient(string.IsNullOrWhiteSpace(key)
                ? new TelemetryConfiguration()
                : new TelemetryConfiguration(key));
            services.AddSingleton(tc);

            services.AddSingleton(sp => sp.GetRequiredService<IConfiguration>().GetSection("TenantCloudBlobContainerFactoryOptions").Get<TenantCloudBlobContainerFactoryOptions>());

            services.AddTenancyApiOnBlobStorage(this.GetRootTenantStorageConfiguration, this.ConfigureOpenApiHost);
        }

        private BlobStorageConfiguration GetRootTenantStorageConfiguration(IServiceProvider serviceProvider)
        {
            IConfiguration config = serviceProvider.GetRequiredService<IConfiguration>();
            return config.GetSection("RootTenantBlobStorageConfigurationOptions").Get<BlobStorageConfiguration>();
        }

        private void ConfigureOpenApiHost(IOpenApiHostConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "AddTenancyApi callback: config");
            }

            if (config.Documents == null)
            {
                throw new ArgumentNullException(nameof(config.Documents), "AddTenancyApi callback: config.Documents");
            }

            config.Documents.AddSwaggerEndpoint();
        }
    }
}
