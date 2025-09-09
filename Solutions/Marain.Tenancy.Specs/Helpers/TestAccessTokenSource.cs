// <copyright file="TestAccessTokenSource.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Helpers;

using System.IdentityModel.Tokens.Jwt;
using System.Threading;
using System.Threading.Tasks;
using Corvus.Identity.ClientAuthentication;

public class TestAccessTokenSource : IServiceIdentityAccessTokenSource
{
    private AccessTokenDetail? token;

    public ValueTask<AccessTokenDetail> GetAccessTokenAsync(AccessTokenRequest requiredTokenCharacteristics, CancellationToken cancellationToken = default)
    {
        this.token ??= CreateTestToken();
        return ValueTask.FromResult(this.token.Value);
    }

    public ValueTask<AccessTokenDetail> GetReplacementForFailedAccessTokenAsync(AccessTokenRequest requiredTokenCharacteristics, CancellationToken cancellationToken = default)
    {
        this.token ??= CreateTestToken();
        return ValueTask.FromResult(this.token.Value);
    }

    private static AccessTokenDetail CreateTestToken()
    {
        JwtSecurityToken jwtToken = TestJwtTokenHelper.CreateToken();
        return GetAccessTokenDetail(jwtToken);
    }

    private static AccessTokenDetail GetAccessTokenDetail(JwtSecurityToken token)
    {
        return new(
            TestJwtTokenHelper.ConvertToTokenString(token),
            token.ValidTo);
    }
}