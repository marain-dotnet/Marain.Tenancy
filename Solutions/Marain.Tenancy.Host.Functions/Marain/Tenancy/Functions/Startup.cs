// <copyright file="Startup.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.WebJobs.Hosting.WebJobsStartup(typeof(Marain.Tenancy.ControlHost.Startup))]

namespace Marain.Tenancy.ControlHost
{
    using System;

    using Corvus.Storage.Azure.BlobStorage;

    using Menes;

    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Startup code for the Function.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <inheritdoc/>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            IServiceCollection services = builder.Services;
            IConfiguration configuration = builder.GetContext().Configuration;

            services.AddApplicationInsightsInstrumentationTelemetry();

            services.AddLogging();

            BlobContainerConfiguration rootStorageConfiguration = configuration
                .GetSection("RootBlobStorageConfiguration")
                .Get<BlobContainerConfiguration>();
            services.AddTenantStoreOnAzureBlobStorage(rootStorageConfiguration);
            services.AddTenancyApiWithOpenApiActionResultHosting(this.ConfigureOpenApiHost);
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