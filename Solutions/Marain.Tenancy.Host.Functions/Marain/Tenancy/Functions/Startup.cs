// <copyright file="Startup.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

[assembly: Microsoft.Azure.WebJobs.Hosting.WebJobsStartup(typeof(Marain.Tenancy.ControlHost.Startup))]

namespace Marain.Tenancy.ControlHost
{
    using System;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Serilog;
    using Serilog.Filters;

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

            IConfigurationRoot root = Configure(services);

            services.AddTenancyApi(root, config =>
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
            });
        }

        private static IConfigurationRoot Configure(IServiceCollection services)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);

            IConfigurationRoot root = configurationBuilder.Build();
            services.AddSingleton(root);
            return root;
        }
    }
}
