// <copyright file="Startup.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Host.AspNetCore
{
    using System;

    using Corvus.Storage.Azure.BlobStorage;

    using Menes;
    using Menes.Auditing.AuditLogSinks.Development;
    using Menes.Hosting.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    /// <summary>
    /// Web host startup class.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Called by ASP.NET to create our startup class.
        /// </summary>
        /// <param name="configuration">Application configuration settings.</param>
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        /// <summary>
        /// Called by ASP.NET so that we can add services to the DI container.
        /// </summary>
        /// <param name="services">DI service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTenancyApiWithAspNetPipelineHosting(ConfigureOpenApiHost);
            services.AddOpenApiAuditing();

            BlobContainerConfiguration rootStorageConfiguration = this.Configuration
                .GetSection("RootBlobStorageConfiguration")
                .Get<BlobContainerConfiguration>();

            services.AddTenantStoreOnAzureBlobStorage(rootStorageConfiguration);

#if DEBUG
            services.AddAuditLogSink<ConsoleAuditLogSink>();
#endif
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Enables us to configure the pipeline.</param>
        /// <param name="env">Information about our host environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMenesCatchAll();
        }

        // TODO: consolidate with functions startup code.
        // This fixes a bug from that - the 2nd exception handler was wrong on two counts:
        //  1. wrong exception type: if config is non-null and config.Documents is null, that's
        //      not ArgumentNullException
        //  2. wrong argument order: we had the nameof and message flipped
        // In any case, this startup is likely to be needed by any host, so we should put it
        // somewhere common.
        private static void ConfigureOpenApiHost(IOpenApiHostConfiguration config)
        {
            ArgumentNullException.ThrowIfNull(config);

            if (config.Documents is null)
            {
                throw new ArgumentException("AddTenancyApi callback: config.Documents", nameof(config));
            }

            config.Documents.AddSwaggerEndpoint();
        }
    }
}