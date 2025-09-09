// <copyright file="TestJwtTokenHelper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Helpers;

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

/// <summary>
/// helper class for generating test tokens when needed.
/// </summary>
public static class TestJwtTokenHelper
{
    /// <summary>
    /// Creates a fake token.
    /// </summary>
    /// <param name="userId">The User Id to add to the token.</param>
    /// <param name="email">The email address to add to the token.</param>
    /// <param name="roles">The list of roles to add to the token.</param>
    /// <returns>A token string.</returns>
    public static string CreateToken(string userId = "test-user", string email = "test@example.com", params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
        };

        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            expires: DateTime.UtcNow.AddHours(1),
            claims: claims,
            signingCredentials: null); // No signature required due to validation bypass

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}