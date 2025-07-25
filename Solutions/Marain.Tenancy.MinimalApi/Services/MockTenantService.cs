// <copyright file="MockTenantService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Services;

using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Nodes;
using Marain.Tenancy.MinimalApi.Models;
using Marain.Tenancy.MinimalApi.Validation;
using Microsoft.Extensions.Logging;

/// <summary>
/// Mock implementation of tenant service for development and testing.
/// This will be replaced with the actual implementation once storage dependencies are available.
/// </summary>
public sealed class MockTenantService : ITenantService
{
    private static readonly FrozenDictionary<string, TenantData> MockTenants = new Dictionary<string, TenantData>
    {
        ["f26450ab-1818-4b64-8c06-ed47a31e0d8e"] = new(
            "f26450ab-1818-4b64-8c06-ed47a31e0d8e",
            "Root Tenant",
            "application/vnd.marain.tenant",
            new Dictionary<string, object> { ["description"] = "Root tenant for the system" },
            null,
            "\"1234567890\""),
        ["550e8400-e29b-41d4-a716-446655440000"] = new(
            "550e8400-e29b-41d4-a716-446655440000",
            "Development Tenant",
            "application/vnd.marain.tenant",
            new Dictionary<string, object> { ["description"] = "Development environment tenant", ["environment"] = "development" },
            "f26450ab-1818-4b64-8c06-ed47a31e0d8e",
            "\"1234567891\""),
        ["550e8400-e29b-41d4-a716-446655440001"] = new(
            "550e8400-e29b-41d4-a716-446655440001",
            "Production Tenant",
            "application/vnd.marain.tenant",
            new Dictionary<string, object> { ["description"] = "Production environment tenant", ["environment"] = "production" },
            "f26450ab-1818-4b64-8c06-ed47a31e0d8e",
            "\"1234567892\"")
    }.ToFrozenDictionary();

    private readonly ILogger<MockTenantService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockTenantService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public MockTenantService(ILogger<MockTenantService> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public Task<TenantServiceResult<TenantResponse>> GetTenantAsync(string tenantId, string? etag = null, CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Getting tenant {TenantId}", tenantId);

        if (!MockTenants.TryGetValue(tenantId, out var tenantData))
        {
            return Task.FromResult(TenantServiceResult<TenantResponse>.NotFound($"Tenant with ID '{tenantId}' not found"));
        }

        // Handle conditional requests with ETag
        if (!string.IsNullOrEmpty(etag) && etag == tenantData.ETag)
        {
            return Task.FromResult(TenantServiceResult<TenantResponse>.NotModified());
        }

        var response = new TenantResponse
        {
            Id = tenantData.Id,
            Name = tenantData.Name,
            ContentType = tenantData.ContentType,
            Properties = tenantData.Properties,
            Links = CreateTenantLinks(tenantData.Id)
        };

        return Task.FromResult(TenantServiceResult<TenantResponse>.Success(response, tenantData.ETag));
    }

    /// <inheritdoc/>
    public Task<TenantServiceResult<TenantResponse>> CreateChildTenantAsync(
        string parentTenantId,
        string tenantName,
        string? wellKnownChildTenantGuid = null,
        CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Creating child tenant '{TenantName}' under parent {ParentTenantId}", tenantName, parentTenantId);

        if (!MockTenants.ContainsKey(parentTenantId))
        {
            return Task.FromResult(TenantServiceResult<TenantResponse>.NotFound($"Parent tenant with ID '{parentTenantId}' not found"));
        }

        var childTenantId = wellKnownChildTenantGuid ?? Guid.NewGuid().ToString();

        if (MockTenants.ContainsKey(childTenantId))
        {
            return Task.FromResult(TenantServiceResult<TenantResponse>.Conflict($"A tenant with ID '{childTenantId}' already exists"));
        }

        var response = new TenantResponse
        {
            Id = childTenantId,
            Name = tenantName,
            ContentType = "application/vnd.marain.tenant",
            Properties = new Dictionary<string, object> { ["parent"] = parentTenantId },
            Links = CreateTenantLinks(childTenantId)
        };

        return Task.FromResult(TenantServiceResult<TenantResponse>.Success(response, "\"" + DateTimeOffset.UtcNow.Ticks + "\""));
    }

    /// <inheritdoc/>
    public Task<TenantServiceResult<ChildTenantsResponse>> GetChildTenantsAsync(
        string parentTenantId,
        int? maxItems = null,
        string? continuationToken = null,
        CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Getting child tenants for parent {ParentTenantId}", parentTenantId);

        if (!MockTenants.ContainsKey(parentTenantId))
        {
            return Task.FromResult(TenantServiceResult<ChildTenantsResponse>.NotFound($"Parent tenant with ID '{parentTenantId}' not found"));
        }

        var childTenants = MockTenants.Values
            .Where(t => t.ParentId == parentTenantId)
            .Select(t => new TenantResponse
            {
                Id = t.Id,
                Name = t.Name,
                ContentType = t.ContentType,
                Properties = t.Properties,
                Links = CreateTenantLinks(t.Id)
            })
            .ToList();

        var maxCount = maxItems ?? 20;
        var startIndex = 0;

        if (!string.IsNullOrEmpty(continuationToken) && int.TryParse(continuationToken, out var tokenIndex))
        {
            startIndex = tokenIndex;
        }

        var pagedTenants = childTenants.Skip(startIndex).Take(maxCount).ToList();
        var nextContinuationToken = startIndex + maxCount < childTenants.Count ? (startIndex + maxCount).ToString() : null;

        var response = new ChildTenantsResponse
        {
            Embedded = new ChildTenantsEmbedded
            {
                Tenants = pagedTenants
            },
            ContinuationToken = nextContinuationToken,
            Links = CreateChildTenantsLinks(parentTenantId)
        };

        return Task.FromResult(TenantServiceResult<ChildTenantsResponse>.Success(response));
    }

    /// <inheritdoc/>
    public Task<TenantServiceResult<TenantResponse>> UpdateTenantAsync(
        string tenantId,
        string jsonPatchDocument,
        CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Updating tenant {TenantId}", tenantId);

        if (!MockTenants.TryGetValue(tenantId, out var tenantData))
        {
            return Task.FromResult(TenantServiceResult<TenantResponse>.NotFound($"Tenant with ID '{tenantId}' not found"));
        }

        // Parse and validate JSON Patch document
        var validationResult = JsonPatchValidator.ValidateJsonPatch(jsonPatchDocument);
        if (!validationResult.IsValid)
        {
            return Task.FromResult(TenantServiceResult<TenantResponse>.ValidationError(validationResult.ErrorMessage!));
        }

        // In a real implementation, we would apply the JSON Patch operations
        // For now, we'll simulate an update by returning the tenant with updated ETag
        var response = new TenantResponse
        {
            Id = tenantData.Id,
            Name = tenantData.Name,
            ContentType = tenantData.ContentType,
            Properties = tenantData.Properties,
            Links = CreateTenantLinks(tenantData.Id)
        };

        var newETag = "\"" + DateTimeOffset.UtcNow.Ticks + "\"";
        return Task.FromResult(TenantServiceResult<TenantResponse>.Success(response, newETag));
    }

    /// <inheritdoc/>
    public Task<TenantServiceResult> DeleteChildTenantAsync(
        string parentTenantId,
        string childTenantId,
        CancellationToken cancellationToken = default)
    {
        this.logger.LogInformation("Deleting child tenant {ChildTenantId} from parent {ParentTenantId}", childTenantId, parentTenantId);

        if (!MockTenants.ContainsKey(parentTenantId))
        {
            return Task.FromResult(TenantServiceResult.NotFound($"Parent tenant with ID '{parentTenantId}' not found"));
        }

        if (!MockTenants.TryGetValue(childTenantId, out var childTenant) || childTenant.ParentId != parentTenantId)
        {
            return Task.FromResult(TenantServiceResult.NotFound($"Child tenant with ID '{childTenantId}' not found under parent '{parentTenantId}'"));
        }

        // In a real implementation, we would delete the tenant from storage
        return Task.FromResult(TenantServiceResult.Success());
    }

    /// <summary>
    /// Creates HAL-style links for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The links dictionary.</returns>
    private static FrozenDictionary<string, LinkResponse> CreateTenantLinks(string tenantId)
    {
        return new Dictionary<string, LinkResponse>
        {
            ["self"] = new() { Href = $"/{tenantId}/marain/tenant" },
            ["children"] = new() { Href = $"/{tenantId}/marain/tenant/children" }
        }.ToFrozenDictionary();
    }

    /// <summary>
    /// Creates HAL-style links for child tenants collection.
    /// </summary>
    /// <param name="parentTenantId">The parent tenant identifier.</param>
    /// <returns>The links dictionary.</returns>
    private static FrozenDictionary<string, LinkResponse> CreateChildTenantsLinks(string parentTenantId)
    {
        return new Dictionary<string, LinkResponse>
        {
            ["self"] = new() { Href = $"/{parentTenantId}/marain/tenant/children" },
            ["parent"] = new() { Href = $"/{parentTenantId}/marain/tenant" }
        }.ToFrozenDictionary();
    }

    /// <summary>
    /// Represents mock tenant data.
    /// </summary>
    /// <param name="Id">The tenant identifier.</param>
    /// <param name="Name">The tenant name.</param>
    /// <param name="ContentType">The content type.</param>
    /// <param name="Properties">The tenant properties.</param>
    /// <param name="ParentId">The parent tenant identifier.</param>
    /// <param name="ETag">The ETag value.</param>
    private sealed record TenantData(
        string Id,
        string Name,
        string ContentType,
        Dictionary<string, object>? Properties,
        string? ParentId,
        string ETag);
}