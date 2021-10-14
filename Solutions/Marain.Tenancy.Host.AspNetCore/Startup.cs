using Corvus.Azure.Storage.Tenancy;

using Menes;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Marain.Tenancy.Host.AspNetCore
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(sp => sp.GetRequiredService<IConfiguration>().GetSection("TenantCloudBlobContainerFactoryOptions").Get<TenantCloudBlobContainerFactoryOptions>());

            services.AddTenancyApiOnBlobStorage(this.GetRootTenantStorageConfiguration, this.ConfigureOpenApiHost);

            services.AddOpenApiHttpRequestHosting<SimpleOpenApiContext>(host =>
            {
               // LoadDocuments(host);
                //host.ConfigureLinkRelations();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
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
