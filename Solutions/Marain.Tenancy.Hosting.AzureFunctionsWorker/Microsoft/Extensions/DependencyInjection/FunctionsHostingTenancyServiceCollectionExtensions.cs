// <copyright file="FunctionsHostingTenancyServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection;

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using Corvus.Extensions;
using Corvus.Tenancy;

using Marain.Tenancy.OpenApi;
using Marain.Tenancy.OpenApi.Configuration;
using Marain.Tenancy.OpenApi.Mappers;

using Menes;
using Menes.Hosting.AzureFunctionsWorker;

using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Extension methods for configuring DI for the Marain Open API services when running in
/// an Azure isolated function.
/// </summary>
public static class FunctionsHostingTenancyServiceCollectionExtensions
{
    /// <summary>
    /// Add services required by the Operations Status API.
    /// </summary>
    /// <typeparam name="TContext">Menes open API context type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureHost">Optional callback for additional host configuration.</param>
    /// <returns>The service collection, to enable chaining.</returns>
    public static IServiceCollection AddTenancyApiWithIsolatedAzureFunctionsHosting<TContext>(
        this IServiceCollection services,
        Action<IOpenApiHostConfiguration>? configureHost = null)
        where TContext : class, IOpenApiContext, new()
    {
        if (services.Any(s => typeof(TenancyService).IsAssignableFrom(s.ServiceType)))
        {
            return services;
        }

        services.AddEverythingExceptHosting();

        services.AddOpenApiAzureFunctionsWorkerHosting<SimpleOpenApiContext>(
            (config) =>
            {
                config.Documents.RegisterOpenApiServiceWithEmbeddedDefinition<TenancyService>();
                configureHost?.Invoke(config);
            },
            env =>
            {
                // Menes' uses its own private serializer configuration - it won't use whatever
                // we've registered in DI. This is used for all Menes converters, and also for
                // the HalDocumentFactory. That affects the tenancy service because it uses HAL
                // documents in its mappers.
                // This callback is the only place in which we have the opportunity to modify the
                // relevant serializer settings.
                ServiceCollection serializerServices = new();
                serializerServices.AddJsonSerializerOptionsProvider();
                serializerServices.AddJsonPropertyBagFactory();
                serializerServices.AddJsonCultureInfoConverter();
                serializerServices.AddJsonDateTimeOffsetToIso8601AndUnixTimeConverter();
                IServiceProvider serializerSp = serializerServices.BuildServiceProvider();

                env.SerializerOptions.Converters.AddRange(serializerSp.GetServices<JsonConverter>());
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
        // ...although it's not clear whether we need them here, as well as in the environment configuration
        // above.
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