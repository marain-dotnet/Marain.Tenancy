// <copyright file="TenancyServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System.Linq;
    using Corvus.Tenancy;
    using Marain.Tenancy.OpenApi;
    using Marain.Tenancy.OpenApi.Configuration;
    using Marain.Tenancy.OpenApi.Mappers;
    using Menes;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Extension methods for configuring DI for the Operations Open API services.
    /// </summary>
    public static class TenancyServiceCollectionExtensions
    {
        /// <summary>
        /// Add services required by the Operations Status API.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection, to enable chaining.</returns>
        public static IServiceCollection AddTenancyApi(this IServiceCollection services)
        {
            if (services.Any(s => typeof(TenancyService).IsAssignableFrom(s.ServiceType)))
            {
                return services;
            }

            // This has to be done first to ensure that the HalDocumentConverter beats the ContentConverter
            services.AddHalDocumentMapper<ITenant, TenantMapper>();
            services.AddHalDocumentMapper<TenantCollectionResult, TenantCollectionResultMapper>();

            services.AddLogging();

            services.AddSingleton<TenancyService>();
            services.AddSingleton<IOpenApiService, TenancyService>(s => s.GetRequiredService<TenancyService>());

            // v2.0+ of Menes no longer registers all of the JsonConverters from Corvus.Extensions.Newtonsoft.Json. To
            // avoid the risk of breaking existing serialized data, we need to manually add them.
            services.AddJsonNetSerializerSettingsProvider();
            services.AddJsonNetPropertyBag();
            services.AddJsonNetCultureInfoConverter();
            services.AddJsonNetDateTimeOffsetToIso8601AndUnixTimeConverter();
            services.AddSingleton<JsonConverter>(new StringEnumConverter(true));

            // Add caching config.
            services.AddSingleton(
                sp => sp.GetRequiredService<IConfiguration>()
                        .GetSection("TenantCacheConfiguration")
                        .Get<TenantCacheConfiguration>() ?? new TenantCacheConfiguration());

            services.AddOpenApiAspNetPipelineHosting<SimpleOpenApiContext>(
                config =>
                {
                    config.Documents.RegisterOpenApiServiceWithEmbeddedDefinition<TenancyService>();
                    config.Documents.AddSwaggerEndpoint();
                });

            return services;
        }
    }
}
