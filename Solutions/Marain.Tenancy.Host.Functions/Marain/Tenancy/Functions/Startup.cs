// <copyright file="Startup.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Functions
{
    using System;

    using Corvus.Storage.Azure.BlobStorage;

    using Menes;

    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Startup code for the Function.
    /// </summary>
    public static class Startup
    {
        /// <summary>
        /// Di initialization.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Configuration.</param>
        public static void Configure(
            IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton(new TelemetryClient(new Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration()));
            services.AddApplicationInsightsInstrumentationTelemetry();

            services.AddLogging();

            BlobContainerConfiguration rootStorageConfiguration = configuration
                .GetSection("RootBlobStorageConfiguration")
                .Get<BlobContainerConfiguration>();
            services.AddTenantStoreOnAzureBlobStorage(rootStorageConfiguration);

            services.AddTenancyApiWithIsolatedAzureFunctionsHosting<SimpleOpenApiContext>(
                ConfigureOpenApiHost);
        }

        /// <summary>
        /// OpenApi document configuration.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="config"/> is null.
        /// </exception>
        private static void ConfigureOpenApiHost(IOpenApiHostConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);

            if (config.Documents == null)
            {
                throw new ArgumentNullException(nameof(config.Documents), "AddTenancyApi callback: config.Documents");
            }

            config.Documents.AddSwaggerEndpoint();
        }
    }
}