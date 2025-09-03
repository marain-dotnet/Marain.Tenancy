// <copyright file="TokenCredentialHandler.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Clients;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Corvus.Identity.ClientAuthentication;

/// <summary>
/// HTTP message handler that automatically adds Azure access tokens to requests.
/// </summary>
/// <param name="accessTokenSource">The token source for obtaining access tokens.</param>
/// <param name="resourceIdForMsiAuthentication">The resource ID to use for MSI authentication.</param>
public class TokenCredentialHandler(IServiceIdentityAccessTokenSource accessTokenSource, string resourceIdForMsiAuthentication) : DelegatingHandler
{
    private readonly SemaphoreSlim tokenLock = new(1, 1);
    private AccessTokenDetail? cachedToken;

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await this.EnsureValidTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.cachedToken!.Value.AccessToken);

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        // Handle 401 by refreshing token and retrying once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            await this.RefreshTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.cachedToken!.Value.AccessToken);
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.tokenLock?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task EnsureValidTokenAsync(CancellationToken cancellationToken)
    {
        if (this.cachedToken.HasValue && this.cachedToken.Value.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return;
        }

        await this.RefreshTokenAsync(cancellationToken);
    }

    private async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        await this.tokenLock.WaitAsync(cancellationToken);
        try
        {
            AccessTokenRequest request = new([$"{resourceIdForMsiAuthentication}/.default"]);
            this.cachedToken = await accessTokenSource.GetAccessTokenAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this.tokenLock.Release();
        }
    }
}