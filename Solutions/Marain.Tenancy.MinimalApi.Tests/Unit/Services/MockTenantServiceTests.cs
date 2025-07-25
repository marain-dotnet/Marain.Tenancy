// <copyright file="MockTenantServiceTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Tests.Unit.Services;

using System;
using System.Threading.Tasks;
using Marain.Tenancy.MinimalApi.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

/// <summary>
/// Unit tests for <see cref="MockTenantService"/>.
/// </summary>
[TestClass]
public sealed class MockTenantServiceTests
{
    private readonly ILogger<MockTenantService> logger = NullLogger<MockTenantService>.Instance;
    private readonly MockTenantService service;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockTenantServiceTests"/> class.
    /// </summary>
    public MockTenantServiceTests()
    {
        this.service = new MockTenantService(this.logger);
    }

    /// <summary>
    /// Test getting an existing tenant returns success.
    /// </summary>
    [TestMethod]
    public async Task GetTenantAsync_WithExistingTenant_ShouldReturnSuccess()
    {
        // Arrange
        var tenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";

        // Act
        var result = await this.service.GetTenantAsync(tenantId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Id.ShouldBe(tenantId);
        result.Data.Name.ShouldBe("Root Tenant");
        result.Data.ContentType.ShouldBe("application/vnd.marain.tenant");
        result.ETag.ShouldNotBeNull();
    }

    /// <summary>
    /// Test getting a non-existent tenant returns not found.
    /// </summary>
    [TestMethod]
    public async Task GetTenantAsync_WithNonExistentTenant_ShouldReturnNotFound()
    {
        // Arrange
        var tenantId = "non-existent-tenant";

        // Act
        var result = await this.service.GetTenantAsync(tenantId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorType.ShouldBe(TenantServiceErrorType.NotFound);
        result.ErrorMessage.ShouldBe($"Tenant with ID '{tenantId}' not found");
        result.Data.ShouldBeNull();
    }

    /// <summary>
    /// Test getting a tenant with matching ETag returns not modified.
    /// </summary>
    [TestMethod]
    public async Task GetTenantAsync_WithMatchingETag_ShouldReturnNotModified()
    {
        // Arrange
        var tenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var etag = "\"1234567890\""; // Known ETag from mock data

        // Act
        var result = await this.service.GetTenantAsync(tenantId, etag);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorType.ShouldBe(TenantServiceErrorType.NotModified);
        result.Data.ShouldBeNull();
    }

    /// <summary>
    /// Test creating a child tenant with valid parameters returns success.
    /// </summary>
    [TestMethod]
    public async Task CreateChildTenantAsync_WithValidParameters_ShouldReturnSuccess()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var tenantName = "TestChildTenant";

        // Act
        var result = await this.service.CreateChildTenantAsync(parentTenantId, tenantName);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Name.ShouldBe(tenantName);
        result.Data.ContentType.ShouldBe("application/vnd.marain.tenant");
        result.Data.Properties.ShouldNotBeNull();
        result.Data.Properties["parent"].ShouldBe(parentTenantId);
        result.ETag.ShouldNotBeNull();
    }

    /// <summary>
    /// Test creating a child tenant with well-known GUID uses provided GUID.
    /// </summary>
    [TestMethod]
    public async Task CreateChildTenantAsync_WithWellKnownGuid_ShouldUseProvidedGuid()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var tenantName = "TestChildTenant";
        var wellKnownGuid = Guid.NewGuid().ToString();

        // Act
        var result = await this.service.CreateChildTenantAsync(parentTenantId, tenantName, wellKnownGuid);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Id.ShouldBe(wellKnownGuid);
        result.Data.Name.ShouldBe(tenantName);
    }

    /// <summary>
    /// Test creating a child tenant with non-existent parent returns not found.
    /// </summary>
    [TestMethod]
    public async Task CreateChildTenantAsync_WithNonExistentParent_ShouldReturnNotFound()
    {
        // Arrange
        var parentTenantId = "non-existent-parent";
        var tenantName = "TestChildTenant";

        // Act
        var result = await this.service.CreateChildTenantAsync(parentTenantId, tenantName);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorType.ShouldBe(TenantServiceErrorType.NotFound);
        result.ErrorMessage.ShouldBe($"Parent tenant with ID '{parentTenantId}' not found");
    }

    /// <summary>
    /// Test creating a child tenant with existing ID returns conflict.
    /// </summary>
    [TestMethod]
    public async Task CreateChildTenantAsync_WithExistingId_ShouldReturnConflict()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var tenantName = "TestChildTenant";
        var existingTenantId = "550e8400-e29b-41d4-a716-446655440000"; // Existing tenant in mock data

        // Act
        var result = await this.service.CreateChildTenantAsync(parentTenantId, tenantName, existingTenantId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorType.ShouldBe(TenantServiceErrorType.Conflict);
        result.ErrorMessage.ShouldBe($"A tenant with ID '{existingTenantId}' already exists");
    }

    /// <summary>
    /// Test getting child tenants for existing parent returns success.
    /// </summary>
    [TestMethod]
    public async Task GetChildTenantsAsync_WithExistingParent_ShouldReturnSuccess()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";

        // Act
        var result = await this.service.GetChildTenantsAsync(parentTenantId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Embedded.ShouldNotBeNull();
        result.Data.Embedded.Tenants.ShouldNotBeNull();
        result.Data.Embedded.Tenants.Count.ShouldBe(2); // Two child tenants in mock data
    }

    /// <summary>
    /// Test getting child tenants with pagination returns limited results.
    /// </summary>
    [TestMethod]
    public async Task GetChildTenantsAsync_WithMaxItems_ShouldReturnLimitedResults()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var maxItems = 1;

        // Act
        var result = await this.service.GetChildTenantsAsync(parentTenantId, maxItems);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Embedded.ShouldNotBeNull();
        result.Data.Embedded.Tenants.ShouldNotBeNull();
        result.Data.Embedded.Tenants.Count.ShouldBe(maxItems);
        result.Data.ContinuationToken.ShouldNotBeNull(); // Should have continuation token
    }

    /// <summary>
    /// Test updating an existing tenant returns success.
    /// </summary>
    [TestMethod]
    public async Task UpdateTenantAsync_WithExistingTenant_ShouldReturnSuccess()
    {
        // Arrange
        var tenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var jsonPatch = """[{ "op": "replace", "path": "/properties/description", "value": "Updated" }]""";

        // Act
        var result = await this.service.UpdateTenantAsync(tenantId, jsonPatch);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldNotBeNull();
        result.Data.Id.ShouldBe(tenantId);
        result.ETag.ShouldNotBeNull();
    }

    /// <summary>
    /// Test updating a non-existent tenant returns not found.
    /// </summary>
    [TestMethod]
    public async Task UpdateTenantAsync_WithNonExistentTenant_ShouldReturnNotFound()
    {
        // Arrange
        var tenantId = "non-existent-tenant";
        var jsonPatch = """[{ "op": "replace", "path": "/name", "value": "Updated" }]""";

        // Act
        var result = await this.service.UpdateTenantAsync(tenantId, jsonPatch);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorType.ShouldBe(TenantServiceErrorType.NotFound);
        result.ErrorMessage.ShouldBe($"Tenant with ID '{tenantId}' not found");
    }

    /// <summary>
    /// Test updating with invalid JSON Patch returns validation error.
    /// </summary>
    [TestMethod]
    public async Task UpdateTenantAsync_WithInvalidJsonPatch_ShouldReturnValidationError()
    {
        // Arrange
        var tenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var invalidJsonPatch = """[{ "op": "invalid", "path": "/name", "value": "Updated" }]""";

        // Act
        var result = await this.service.UpdateTenantAsync(tenantId, invalidJsonPatch);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorType.ShouldBe(TenantServiceErrorType.ValidationError);
        result.ErrorMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Test deleting an existing child tenant returns success.
    /// </summary>
    [TestMethod]
    public async Task DeleteChildTenantAsync_WithExistingChild_ShouldReturnSuccess()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var childTenantId = "550e8400-e29b-41d4-a716-446655440000";

        // Act
        var result = await this.service.DeleteChildTenantAsync(parentTenantId, childTenantId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Test deleting a non-existent child tenant returns not found.
    /// </summary>
    [TestMethod]
    public async Task DeleteChildTenantAsync_WithNonExistentChild_ShouldReturnNotFound()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var childTenantId = "non-existent-child";

        // Act
        var result = await this.service.DeleteChildTenantAsync(parentTenantId, childTenantId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.ErrorType.ShouldBe(TenantServiceErrorType.NotFound);
        result.ErrorMessage!.ShouldContain("not found under parent");
    }
}