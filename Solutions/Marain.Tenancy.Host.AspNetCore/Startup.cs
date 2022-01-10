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

    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
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

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

            if (config.Documents == null)
            {
                throw new ArgumentException("AddTenancyApi callback: config.Documents", nameof(config));
            }

            config.Documents.AddSwaggerEndpoint();
        }
    }
}
