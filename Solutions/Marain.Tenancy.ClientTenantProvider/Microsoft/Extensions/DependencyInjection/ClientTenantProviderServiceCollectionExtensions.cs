// <copyright file="ClientTenantProviderServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Corvus.ContentHandling;
using Corvus.Json;
using Corvus.Tenancy;
using Marain.Tenancy;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Models;
using Marain.Tenancy.Mappers;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register the Root tenant with the service collection.
/// </summary>
public static class ClientTenantProviderServiceCollectionExtensions
{
    /// <summary>
    /// Adds the root tenant to the collection, using the <see cref="TenancyApiClient"/>.
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
        ////services.AddSingleton(s =>
        ////{
        ////    throw new NotImplementedException();
        ////    ITenancyService tenancyService = s.GetRequiredService<ITenancyService>();
        ////    ITenantMapper tenantMapper = s.GetRequiredService<ITenantMapper>();
        ////    IPropertyBagFactory propertyBagFactory = s.GetRequiredService<IPropertyBagFactory>();
        ////    TenantResponse? rootTenantResponse = tenancyService.GetTenantAsync(RootTenant.RootTenantId).GetAwaiter().GetResult();
        ////    ArgumentNullException.ThrowIfNull(rootTenantResponse, "Unable to retrieve root tenant from service");
        ////    ITenant fetchedRootTenant = tenantMapper.MapTenant(rootTenantResponse);
        ////    var localRootTenant = new RootTenant(propertyBagFactory);
        ////    IReadOnlyDictionary<string, object> propertiesToSetOrAdd = fetchedRootTenant.Properties.AsDictionary();
        ////    localRootTenant.UpdateProperties(propertiesToSetOrAdd);
        ////    return localRootTenant;
        ////});

        return services;
    }

    /// <summary>
    /// Adds services a Kiota client-based implementation of <see cref="ITenantProvider"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">The base URL for the Tenancy API.</param>
    /// <returns>The modified service collection.</returns>
    /// <remarks>
    /// <para>
    /// This method registers the Kiota-based tenancy client and related services.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddTenantProviderServiceClient(
        this IServiceCollection services,
        string baseUrl)
    {
        if (services.Any(s => typeof(ITenantProvider).IsAssignableFrom(s.ServiceType)))
        {
            return services;
        }

        services.AddTenancyClient(baseUrl);
        services.AddTenantServiceClientRootTenant();
        services.AddSingleton<ITenantMapper, TenantMapper>();
        services.AddSingleton<ITenantProvider, ClientTenantProvider>();
        services.AddSingleton<ITenantStore, ClientTenantStore>();
        return services;
    }
}