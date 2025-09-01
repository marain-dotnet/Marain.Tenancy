// <copyright file="ClientTenantProviderServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using Azure;
using Corvus.ContentHandling;
using Corvus.Json;
using Corvus.Tenancy;
using Marain.Clients;
using Marain.Tenancy;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Resources;
using Marain.Tenancy.Mappers;

/// <summary>
/// Extensions to register the Root tenant with the service collection.
/// </summary>
public static class ClientTenantProviderServiceCollectionExtensions
{
    /// <summary>
    /// Adds the root tenant to the collection, using the <see cref="ITenancyClient"/>.
    /// </summary>
    /// <param name="services">The service collection to which to add the root tenant.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddTenantServiceClientRootTenant(this IServiceCollection services)
    {
        if (services.Any(s => typeof(RootTenant).IsAssignableFrom(s.ServiceType)))
        {
            return services;
        }

        services.AddContent(contentFactory => contentFactory.RegisterTransientContent<Tenant>());

        // Construct a root tenant from the tenant retrieved from the service, using the
        // root tenant ID.
        services.AddSingleton(s =>
        {
            ITenancyClient tenancyService = s.GetRequiredService<ITenancyClient>();
            ITenantMapper tenantMapper = s.GetRequiredService<ITenantMapper>();
            IPropertyBagFactory propertyBagFactory = s.GetRequiredService<IPropertyBagFactory>();

            ApiResponse<TenantResource> rootTenantResponse = tenancyService.GetTenantAsync(RootTenant.RootTenantId).GetAwaiter().GetResult();
            ArgumentNullException.ThrowIfNull(rootTenantResponse, "Unable to retrieve root tenant from service");
            rootTenantResponse.Headers.TryGetKey("etag", out string? etag);
            ITenant fetchedRootTenant = tenantMapper.MapTenant(rootTenantResponse.Body, etag);
            var localRootTenant = new RootTenant(propertyBagFactory);
            IReadOnlyDictionary<string, object> propertiesToSetOrAdd = fetchedRootTenant.Properties.AsDictionary();
            localRootTenant.UpdateProperties(propertiesToSetOrAdd);
            return localRootTenant;
        });

        return services;
    }

    /// <summary>
    /// Adds services a Kiota client-based implementation of <see cref="ITenantProvider"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    /// <remarks>
    /// <para>
    /// This method registers an implementation of <see cref="ITenantProvider"/> which uses the Kiota-based
    /// tenancy client to access tenant data. You will need to ensure that the tenancy client is also registered.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddTenantProviderServiceClient(
        this IServiceCollection services)
    {
        if (services.Any(s => typeof(ITenantProvider).IsAssignableFrom(s.ServiceType)))
        {
            return services;
        }

        services.AddTenantServiceClientRootTenant();
        services.AddSingleton<ITenantMapper, TenantMapper>();
        services.AddSingleton<ITenantProvider, ClientTenantProvider>();
        services.AddSingleton<ITenantStore, ClientTenantStore>();
        return services;
    }
}