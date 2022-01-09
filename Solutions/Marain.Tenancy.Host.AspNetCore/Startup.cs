namespace Marain.Tenancy.Host.AspNetCore
{
    using Corvus.Storage.Azure.BlobStorage;

    using Menes.Auditing.AuditLogSinks.Development;
    using Menes.Hosting.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTenancyApiWithAspNetPipelineHosting();
            services.AddOpenApiAuditing();

            var sp = services.BuildServiceProvider();

            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();

            BlobContainerConfiguration rootStorageConfiguration = configuration
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
    }
}
