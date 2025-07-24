namespace Marain.Tenancy.Client;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Corvus.Identity.ClientAuthentication.Azure;
using Microsoft.Kiota.Abstractions.Authentication;

internal class CorvusIdentityAccessTokenProvider : IAccessTokenProvider
{
    private readonly IServiceIdentityAzureTokenCredentialSource tokenCredentialSource;

    private AccessToken? currentToken;

    internal CorvusIdentityAccessTokenProvider(IServiceIdentityAzureTokenCredentialSource tokenCredentialSource)
    {
        this.tokenCredentialSource = tokenCredentialSource;
    }

    public AllowedHostsValidator AllowedHostsValidator
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        AccessToken token = await this.GetOrRefreshToken(cancellationToken);
        return token.Token;
    }

    private async Task<AccessToken> GetOrRefreshToken(CancellationToken cancellationToken = default)
    {
        if (currentToken is null || currentToken.Value.ExpiresOn < DateTimeOffset.UtcNow)
        {
            currentToken = await this.GetNewToken(cancellationToken).ConfigureAwait(false);
        }

        return this.currentToken.Value;
    }

    private async Task<AccessToken> GetNewToken(CancellationToken cancellationToken = default)
    {
		TokenCredential tokenCredential = await this.tokenCredentialSource.GetTokenCredentialAsync(cancellationToken);
		TokenRequestContext requestContext = new();
		return await tokenCredential.GetTokenAsync(requestContext, cancellationToken);
	}
}
