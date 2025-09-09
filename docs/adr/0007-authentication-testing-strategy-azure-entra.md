# ADR-002: Authentication Testing Strategy for Azure Entra Integration

**Status**: Proposed
**Date**: 2025-01-04
**Authors**: Claude Code Assistant

## Context

The Marain.Tenancy.Api has been secured with Azure Entra ID authentication using JWT Bearer tokens. All API endpoints now require authentication by default through a global authorization policy. This change has broken the existing integration tests that rely on `ApiWebApplicationFactory` (based on `WebApplicationFactory<Program>`) to create an in-process test server, as they can no longer authenticate against the secured endpoints.

### Current Test Architecture

The existing test suite uses:
- **Reqnroll** (formerly SpecFlow) for BDD-style integration tests
- **ApiWebApplicationFactory** that extends `WebApplicationFactory<Program>`
- **Azurite container** for Azure Blob Storage emulation
- **Direct HTTP calls** to test API endpoints without authentication

The failing tests include:
- API endpoint tests (`TenancyApi.feature`)
- Tenant CRUD operations 
- Swagger endpoint accessibility
- All scenarios that expect 2xx responses now receive 401 Unauthorized

## Problem Statement

Integration tests can no longer access protected API endpoints because:
1. Tests cannot provide valid Azure Entra JWT tokens
2. Real Azure Entra authentication would require:
   - External service dependencies
   - Secret management in test environment
   - Network connectivity to Azure
   - Complex token lifecycle management
3. Tests should be isolated and not dependent on external authentication providers

## Decision

After researching multiple approaches for authentication testing in ASP.NET Core, we recommend implementing **Approach 3: JWT Validation Bypass** as the primary solution, with **Approach 4: Test Authentication Handler** as an alternative for scenarios requiring claim-specific testing.

## Considered Approaches

### Approach 1: Mock JWT Token Generation
```csharp
// Disable JWT validation parameters
services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options => {
    options.TokenValidationParameters.ValidateIssuerSigningKey = false;
    options.TokenValidationParameters.ValidateIssuer = false;
    options.TokenValidationParameters.ValidateAudience = false;
    options.TokenValidationParameters.ValidateLifetime = false;
    options.TokenValidationParameters.RequireSignedTokens = false;
});

// Generate unsigned tokens
private static string GetJwtToken() {
    var token = new JwtSecurityToken(
        issuer: "http://localhost",
        audience: "http://localhost", 
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: null,
        claims: [
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Email, "test@example.com")
        ]
    );
    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

**Pros:**
- Uses actual JWT Bearer authentication flow
- Realistic token structure with claims
- Can test different user scenarios
- Maintains JWT middleware pipeline

**Cons:**
- More complex setup
- Still requires JWT configuration tweaking
- Overhead of token generation and parsing

### Approach 2: Fake Policy Evaluator
```csharp
// Replace IPolicyEvaluator with fake implementation
services.RemoveAll<IPolicyEvaluator>();
services.AddSingleton<IPolicyEvaluator, FakePolicyEvaluator>();

public class FakePolicyEvaluator : IPolicyEvaluator
{
    public Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Email, "test@example.com")
        }, "Test"));
        
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, "Test")));
    }

    public Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, 
        AuthenticateResult authenticationResult, HttpContext context, object resource)
    {
        return Task.FromResult(PolicyAuthorizationResult.Success());
    }
}
```

**Pros:**
- Bypasses entire authentication/authorization pipeline
- Simple implementation
- Fast execution
- Complete control over authorization decisions

**Cons:**
- Doesn't test actual authentication middleware
- Could hide authentication configuration issues
- Less realistic than token-based approaches

### Approach 3: JWT Validation Bypass ⭐ **RECOMMENDED**
```csharp
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    // Existing configuration...
    builder.ConfigureTestServices(services =>
    {
        // Disable JWT validation for tests
        services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                RequireSignedTokens = false,
                SignatureValidator = (token, parameters) => new JwtSecurityToken(token)
            };
        });
    });
}
```

**Pros:**
- ✅ Minimal changes to existing test infrastructure
- ✅ Preserves JWT Bearer authentication scheme
- ✅ Fast and reliable
- ✅ No external dependencies
- ✅ Easy to understand and maintain
- ✅ Allows testing with or without tokens

**Cons:**
- ❌ Doesn't test actual token validation logic
- ❌ Requires unsigned/invalid tokens

### Approach 4: Test Authentication Handler
```csharp
public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
{
    public const string AuthenticationScheme = "Test";
    
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Role, "User")
        };
        
        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);
        
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

// Configuration
services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
    .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(
        TestAuthHandler.AuthenticationScheme, options => { });
```

**Pros:**
- Complete control over authentication scheme
- Can inject specific claims for testing
- Clean separation from production authentication
- Supports dynamic user context per test

**Cons:**
- Requires new authentication scheme
- More code to maintain
- Replaces rather than tests existing auth configuration

### Approach 5: Environment-Based Authentication Disable
```csharp
// In Program.cs
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
    
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
}
```

**Pros:**
- Complete bypass of authentication in test environment
- No test-specific authentication code needed
- Fastest execution

**Cons:**
- ❌ Doesn't test authentication/authorization pipeline at all
- ❌ Requires production code changes
- ❌ Could hide configuration issues
- ❌ Not recommended for security-critical applications

## Recommended Implementation

### Primary Approach: JWT Validation Bypass

Modify the existing `ApiWebApplicationFactory` to disable JWT validation:

```csharp
/// <summary>
/// Custom WebApplicationFactory that can create the API application for testing.
/// </summary>
internal class ApiWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    // ... existing implementation ...

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Existing Azurite configuration...
        this.azuriteContainer.StartAsync().GetAwaiter().GetResult();
        string connectionString = this.azuriteContainer.GetConnectionString();
        Environment.SetEnvironmentVariable("RootBlobStorageConfiguration:ConnectionStringPlainText", connectionString);
        builder.UseEnvironment(Environments.Development);

        // Configure test services to bypass JWT validation
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false, 
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = false,
                    RequireSignedTokens = false,
                    // Accept any token without signature validation
                    SignatureValidator = (token, parameters) => new JwtSecurityToken(token)
                };
            });
        });
    }
}
```

### Test Helper for Token Generation

Create a helper class for generating test tokens when needed:

```csharp
public static class TestJwtTokenHelper
{
    public static string CreateToken(string userId = "test-user", string email = "test@example.com", params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email)
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
            signingCredentials: null // No signature required due to validation bypass
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Updated Test Steps

Modify test steps to include authentication header when needed:

```csharp
private async Task SendGetRequest(string path, string? etag = null)
{
    var request = new HttpRequestMessage(HttpMethod.Get, path);
    
    // Add test JWT token
    string token = TestJwtTokenHelper.CreateToken();
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    if (!string.IsNullOrEmpty(etag))
    {
        request.Headers.Add("If-None-Match", etag);
    }

    HttpResponseMessage response = await ApiWebApplicationFactory.Current.Client.SendAsync(request);
    await this.SetResponseAsync(response);
}
```

## Implementation Plan

### Phase 1: Basic Authentication Bypass
1. ✅ Add JWT validation bypass to `ApiWebApplicationFactory`
2. ✅ Create `TestJwtTokenHelper` utility class
3. ✅ Update existing test methods to include Authorization headers
4. ✅ Run tests to verify basic functionality

### Phase 2: Enhanced Testing Capabilities
1. Add support for different user roles and claims in tests
2. Create test scenarios for authorization edge cases
3. Add negative testing for unauthorized scenarios

### Phase 3: Alternative Implementation (if needed)
1. Implement `TestAuthHandler` approach for complex claim scenarios
2. Add configuration to switch between approaches
3. Document when to use each approach

## Testing the Implementation

### Verification Steps
1. **Existing tests should pass**: All current Reqnroll scenarios should work
2. **Anonymous endpoints still work**: Health check and Swagger endpoints remain accessible
3. **Protected endpoints accept test tokens**: API endpoints accept generated test tokens
4. **Unauthorized requests fail**: Requests without tokens should still return 401

### Example Test Scenarios
```gherkin
Scenario: Get a tenant with valid authentication
  Given I have a valid authentication token
  When I request the tenant with Id 'f26450ab1668784bb327951c8b08f347' from the API  
  Then I receive an 'OK' response

Scenario: Get a tenant without authentication
  Given I do not have an authentication token
  When I request the tenant with Id 'f26450ab1668784bb327951c8b08f347' from the API
  Then I receive an 'Unauthorized' response
```

## Security Considerations

1. **Test Environment Isolation**: JWT validation bypass only applies in test environment
2. **No Production Impact**: Changes are isolated to test configuration
3. **Maintain Authorization Testing**: Can still test authorization policies with different claims
4. **Audit Trail**: Clear separation between test and production authentication

## Migration Impact

### Low Risk Changes
- ✅ Existing test structure remains unchanged
- ✅ No changes to production authentication
- ✅ Additive changes to test infrastructure
- ✅ Backwards compatible with existing tests

### Required Updates
- Modify `ApiWebApplicationFactory.ConfigureWebHost()` 
- Add `TestJwtTokenHelper` utility class
- Update test step methods to include Authorization headers
- Add new test scenarios for authentication edge cases

## Alternative Considerations

If the primary approach doesn't meet all requirements, the **Test Authentication Handler** approach (Approach 4) provides more control over authentication scenarios and can be implemented alongside or instead of JWT validation bypass.

## Decision Rationale

**JWT Validation Bypass** is recommended because:

1. **Minimal Disruption**: Requires minimal changes to existing test infrastructure
2. **Performance**: Fast test execution without token validation overhead  
3. **Reliability**: No external dependencies or complex token management
4. **Maintainability**: Simple to understand and debug
5. **Flexibility**: Can still test different authentication scenarios with custom claims
6. **Industry Standard**: Widely used pattern in ASP.NET Core applications

## Consequences

### Positive Consequences
- ✅ Integration tests work with Azure Entra authentication
- ✅ Fast and reliable test execution
- ✅ No external service dependencies
- ✅ Maintains test isolation
- ✅ Supports different user/claim scenarios
- ✅ Clear separation between test and production auth

### Negative Consequences  
- ❌ Doesn't test actual JWT signature validation
- ❌ Potential to hide authentication configuration issues
- ❌ Requires discipline to ensure adequate auth testing

### Mitigation Strategies
- Add smoke tests that verify production authentication configuration
- Include negative test cases for authorization scenarios  
- Document authentication testing strategy for team awareness
- Consider periodic manual testing with real Azure Entra tokens

## Future Considerations

1. **E2E Testing**: Consider separate E2E tests with real Azure Entra integration
2. **Contract Testing**: Add tests to verify Azure Entra token claims structure
3. **Security Auditing**: Regular review of authentication test coverage
4. **Configuration Validation**: Add tests to verify production auth configuration

## References

- [ASP.NET Core Integration Testing Documentation](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [Testing ASP.NET Core Endpoints with Fake JWT Tokens](https://renatogolia.com/2025/08/01/testing-aspnet-core-endpoints-with-fake-jwt-tokens-and-webapplicationfactory/)
- [Mocking Authentication in ASP.NET Core Integration Tests](https://mazeez.dev/posts/auth-in-integration-tests)
- [Microsoft Identity Web Testing Guidance](https://docs.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)