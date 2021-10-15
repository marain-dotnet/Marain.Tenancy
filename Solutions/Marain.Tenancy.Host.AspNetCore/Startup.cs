namespace Marain.Tenancy.Host.AspNetCore
{
    using Corvus.Azure.Storage.Tenancy;
    using Marain.Tenancy.OpenApi.Configuration;
    using Menes;
    using Menes.Auditing.AuditLogSinks.Development;
    using Menes.Hosting.AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    using System;

    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTenancyApiOnBlobStorage(this.GetRootTenantStorageConfiguration);
            services.AddOpenApiAuditing();

            services.AddSingleton(sp => sp.GetRequiredService<IConfiguration>().GetSection("TenantCloudBlobContainerFactoryOptions").Get<TenantCloudBlobContainerFactoryOptions>());
            services.AddSingleton(
                sp => sp.GetRequiredService<IConfiguration>()
                        .GetSection("TenantCacheConfiguration")
                        .Get<TenantCacheConfiguration>() ?? new TenantCacheConfiguration());
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

        private BlobStorageConfiguration GetRootTenantStorageConfiguration(IServiceProvider serviceProvider)
        {
            IConfiguration config = serviceProvider.GetRequiredService<IConfiguration>();

            BlobStorageConfiguration rootTenantBlobStorageConfig = config.GetSection("RootTenantBlobStorageConfigurationOptions").Get<BlobStorageConfiguration>();

            if (string.IsNullOrEmpty(rootTenantBlobStorageConfig?.AccountName))
            {
                throw new Exception("Missing RootTenantBlobStorageConfigurationOptions");
            }

            return rootTenantBlobStorageConfig;
        }
    }
}
