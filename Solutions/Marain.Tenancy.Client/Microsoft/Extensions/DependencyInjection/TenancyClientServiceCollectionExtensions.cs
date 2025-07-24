namespace Microsoft.Extensions.DependencyInjection;

using System;
using System.Linq;
using Corvus.Identity.ClientAuthentication;
using Marain.Tenancy.Client;
using Marain.Tenancy.Client.Internal;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Bundle;

public static class TenancyClientServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Tenancy client to a service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="enableResponseCaching">Flag indicating whether or not response caching should be enabled for GET operations.</param>
    /// <returns>The modified service collection.</returns>
    /// <remarks>
    /// <para>
    /// This requires the <see cref="TenancyClientOptions"/> to be available from DI in order
    /// to discover the base URI of the Operations control service, and, if required, to
    /// specify the resource id to use when obtaining an authentication token representing the
    /// hosting service's identity.
    /// </para>
    /// <para>
    /// This also requires an implementation of <see cref="IServiceIdentityAccessTokenSource"/>
    /// to be available via DI. This is normally achieved through one of the various
    /// <c>AddServiceIdentityAzureTokenCredentialSource...</c> extension methods available when
    /// you install the <c>Corvus.Identity.Azure</c> NuGet package, but applications are free
    /// to supply alternate implementations.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddTenancyClient(
        this IServiceCollection services,
        bool enableResponseCaching)
    {
        if (services.Any(s => s.ServiceType == typeof(ITenancyService)))
        {
            return services;
        }

        services.AddContentTypeBasedSerializationSupport();

        services.AddSingleton<IAccessTokenProvider, CorvusIdentityAccessTokenProvider>();
        services.AddSingleton(KiotaAuthenticationProviderFactory);
        services.AddSingleton(MarainTenancyClientFactory);
        services.AddSingleton<ITenancyService, TenancyService>();

        return services;
    }

    private static IAuthenticationProvider KiotaAuthenticationProviderFactory(IServiceProvider serviceProvider)
    {
        TenancyClientOptions options = serviceProvider.GetRequiredService<TenancyClientOptions>();

        if (string.IsNullOrEmpty(options.ResourceIdForMsiAuthentication))
        {
            return new AnonymousAuthenticationProvider();
        }

        CorvusIdentityAccessTokenProvider accessTokenProvider = serviceProvider.GetRequiredService<CorvusIdentityAccessTokenProvider>();
        return new BaseBearerTokenAuthenticationProvider(accessTokenProvider);
    }

    private static MarainTenancyClient MarainTenancyClientFactory(IServiceProvider serviceProvider)
    {
        TenancyClientOptions options = serviceProvider.GetRequiredService<TenancyClientOptions>();
        IAuthenticationProvider authProvider = serviceProvider.GetRequiredService<IAuthenticationProvider>();

        // TODO: Add client side caching back in

        // First: build the Kiota based client
        var requestAdapter = new DefaultRequestAdapter(authProvider);
        requestAdapter.BaseUrl = options.TenancyServiceBaseUri;
        return new MarainTenancyClient(requestAdapter);
    }
}
