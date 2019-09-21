﻿// <copyright file="TenancyServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.OpenApi
{
    using System;
    using System.Linq;
    using Corvus.Tenancy;
    using Marain.Tenancy.OpenApi.Mappers;
    using Menes;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension methods for configuring DI for the Operations Open API services.
    /// </summary>
    public static class TenancyServiceCollectionExtensions
    {
        /// <summary>
        /// Add services required by the Operations Status API.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureHost">Optional callback for additional host configuration.</param>
        /// <returns>The service collection, to enable chaining.</returns>
        public static IServiceCollection AddTenancyApi(
            this IServiceCollection services,
            Action<IOpenApiHostConfiguration> configureHost = null)
        {
            if (services.Any(s => typeof(TenancyService).IsAssignableFrom(s.ServiceType)))
            {
                return services;
            }

            // This has to be done first to ensure that the HalDocumentConverter beats the ContentConverter
            services.AddHalDocumentMapper<ITenant, TenantMapper>();
            services.AddHalDocumentMapper<TenantCollectionResult, TenantCollectionResultMapper>();

            services.AddLogging();
            services.AddRootTenant();

            services.AddSingleton<TenancyService>();
            services.AddSingleton<IOpenApiService, TenancyService>(s => s.GetRequiredService<TenancyService>());

            services.AddOpenApiHttpRequestHosting<SimpleOpenApiContext>((config) =>
            {
                config.Documents.RegisterOpenApiServiceWithEmbeddedDefinition<TenancyService>();
                configureHost?.Invoke(config);
            });

            return services;
        }
    }
}
