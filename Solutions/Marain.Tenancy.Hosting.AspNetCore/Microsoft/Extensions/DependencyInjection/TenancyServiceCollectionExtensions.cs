﻿// <copyright file="TenancyServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    using Corvus.Tenancy;
    using Marain.Tenancy.OpenApi;
    using Marain.Tenancy.OpenApi.Configuration;
    using Marain.Tenancy.OpenApi.Mappers;
    using Menes;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Extension methods for configuring DI for the Marain Open API services in hosting environments
    /// using ASP.NET Core types (including the in-process Azure Functions model).
    /// </summary>
    public static class TenancyServiceCollectionExtensions
    {
        /// <summary>
        /// Add services required by the Operations Status API.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureHost">Optional callback for additional host configuration.</param>
        /// <returns>The service collection, to enable chaining.</returns>
        [Obsolete("Use AddTenancyApiWithOpenApiActionResultHosting, or consider changing to AddTenancyApiWithAspNetPipelineHosting")]
        public static IServiceCollection AddTenancyApi(
            this IServiceCollection services,
            Action<IOpenApiHostConfiguration>? configureHost = null)
        {
            return AddTenancyApiWithOpenApiActionResultHosting(services, configureHost);
        }

        /// <summary>
        /// Add services required by the Operations Status API.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureHost">Optional callback for additional host configuration.</param>
        /// <returns>The service collection, to enable chaining.</returns>
        public static IServiceCollection AddTenancyApiWithAspNetPipelineHosting(
            this IServiceCollection services,
            Action<IOpenApiHostConfiguration>? configureHost = null)
        {
            if (services.Any(s => typeof(TenancyService).IsAssignableFrom(s.ServiceType)))
            {
                return services;
            }

            services.AddEverythingExceptHosting();

            services.AddOpenApiAspNetPipelineHosting<SimpleOpenApiContext>((config) =>
            {
                config.Documents.RegisterOpenApiServiceWithEmbeddedDefinition<TenancyService>();
                configureHost?.Invoke(config);
            });

            return services;
        }

        /// <summary>
        /// Add services required by the Operations Status API.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureHost">Optional callback for additional host configuration.</param>
        /// <returns>The service collection, to enable chaining.</returns>
        public static IServiceCollection AddTenancyApiWithOpenApiActionResultHosting(
            this IServiceCollection services,
            Action<IOpenApiHostConfiguration>? configureHost = null)
        {
            if (services.Any(s => typeof(TenancyService).IsAssignableFrom(s.ServiceType)))
            {
                return services;
            }

            services.AddEverythingExceptHosting();

            services.AddOpenApiActionResultHosting<SimpleOpenApiContext>((config) =>
            {
                config.Documents.RegisterOpenApiServiceWithEmbeddedDefinition<TenancyService>();
                configureHost?.Invoke(config);
            });

            return services;
        }

        private static void AddEverythingExceptHosting(this IServiceCollection services)
        {
            // This has to be done first to ensure that the HalDocumentConverter beats the ContentConverter
            services.AddHalDocumentMapper<ITenant, TenantMapper>();
            services.AddHalDocumentMapper<TenantCollectionResult, TenantCollectionResultMapper>();

            services.AddLogging();

            services.AddSingleton<TenancyService>();
            services.AddSingleton<IOpenApiService, TenancyService>(s => s.GetRequiredService<TenancyService>());

            // v2.0+ of Menes no longer registers all of the JsonConverters from Corvus.Extensions.Newtonsoft.Json. To
            // avoid the risk of breaking existing serialized data, we need to manually add them.
            services.AddJsonSerializerOptionsProvider();
            services.AddJsonPropertyBagFactory();
            services.AddJsonCultureInfoConverter();
            services.AddJsonDateTimeOffsetToIso8601AndUnixTimeConverter();
            services.AddSingleton<JsonConverter>(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            // Add caching config.
            services.AddSingleton(
                sp => sp.GetRequiredService<IConfiguration>()
                        .GetSection("TenantCacheConfiguration")
                        .Get<TenantCacheConfiguration>() ?? new TenantCacheConfiguration());
        }
    }
}