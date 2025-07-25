// <copyright file="TenancyApiIntegrationTests.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.MinimalApi.Tests.Integration;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Marain.Tenancy.MinimalApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

/// <summary>
/// Integration tests for the Tenancy API endpoints.
/// </summary>
[TestClass]
public sealed class TenancyApiIntegrationTests : IDisposable
{
    private readonly TenancyApiTestWebApplicationFactory factory;
    private readonly HttpClient client;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenancyApiIntegrationTests"/> class.
    /// </summary>
    public TenancyApiIntegrationTests()
    {
        this.factory = new TenancyApiTestWebApplicationFactory();
        this.client = this.factory.CreateClient();
    }

    /// <summary>
    /// Test that the Swagger/OpenAPI endpoint returns successfully.
    /// </summary>
    [TestMethod]
    public async Task GetSwaggerDefinition_ShouldReturnOk()
    {
        // Act
        var response = await this.client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    /// <summary>
    /// Test getting a tenant that does not exist returns NotFound.
    /// </summary>
    [TestMethod]
    public async Task GetNonExistentTenant_ShouldReturnNotFound()
    {
        // Act
        var response = await this.client.GetAsync("/thistenantdoesnotexist/marain/tenant");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Test getting the root tenant returns the expected data.
    /// </summary>
    [TestMethod]
    public async Task GetRootTenant_ShouldReturnOkWithCorrectData()
    {
        // Arrange
        var rootTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";

        // Act
        var response = await this.client.GetAsync($"/{rootTenantId}/marain/tenant");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var tenant = await response.Content.ReadFromJsonAsync<TenantResponse>();
        tenant.ShouldNotBeNull();
        tenant.Id.ShouldBe(rootTenantId);
        tenant.Name.ShouldBe("Root Tenant");
        tenant.ContentType.ShouldBe("application/vnd.marain.tenant");

        // Should have ETag header
        response.Headers.ETag.ShouldNotBeNull();
    }

    /// <summary>
    /// Test creating a child tenant returns Created with location header.
    /// </summary>
    [TestMethod]
    public async Task CreateChildTenant_ShouldReturnCreatedWithLocationHeader()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var tenantName = "TestTenant";

        // Act
        var response = await this.client.PostAsync(
            $"/{parentTenantId}/marain/tenant?tenantName={tenantName}", 
            null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var tenant = await response.Content.ReadFromJsonAsync<TenantResponse>();
        tenant.ShouldNotBeNull();
        tenant.Name.ShouldBe(tenantName);
        tenant.ContentType.ShouldBe("application/vnd.marain.tenant");

        // Should have ETag header
        response.Headers.ETag.ShouldNotBeNull();
    }

    /// <summary>
    /// Test creating a tenant with a well-known GUID.
    /// </summary>
    [TestMethod]
    public async Task CreateChildTenantWithWellKnownGuid_ShouldUseProvidedGuid()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var tenantName = "TestTenantWithGuid";
        var wellKnownGuid = Guid.NewGuid().ToString();

        // Act
        var response = await this.client.PostAsync(
            $"/{parentTenantId}/marain/tenant?tenantName={tenantName}&wellKnownChildTenantGuid={wellKnownGuid}", 
            null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var tenant = await response.Content.ReadFromJsonAsync<TenantResponse>();
        tenant.ShouldNotBeNull();
        tenant.Id.ShouldBe(wellKnownGuid);
        tenant.Name.ShouldBe(tenantName);
    }

    /// <summary>
    /// Test getting child tenants returns a paginated list.
    /// </summary>
    [TestMethod]
    public async Task GetChildTenants_ShouldReturnPaginatedList()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";

        // Act
        var response = await this.client.GetAsync($"/{parentTenantId}/marain/tenant/children");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var childTenants = await response.Content.ReadFromJsonAsync<ChildTenantsResponse>();
        childTenants.ShouldNotBeNull();
        childTenants.Embedded.ShouldNotBeNull();
        childTenants.Embedded.Tenants.ShouldNotBeNull();
    }

    /// <summary>
    /// Test conditional GET request with matching ETag returns NotModified.
    /// </summary>
    [TestMethod]
    public async Task GetTenantWithMatchingETag_ShouldReturnNotModified()
    {
        // Arrange
        var tenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";

        // First, get the tenant to obtain its ETag
        var firstResponse = await this.client.GetAsync($"/{tenantId}/marain/tenant");
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = firstResponse.Headers.ETag?.Tag;
        etag.ShouldNotBeNull();

        // Act - Make the same request with the ETag
        var request = new HttpRequestMessage(HttpMethod.Get, $"/{tenantId}/marain/tenant");
        request.Headers.Add("If-None-Match", etag);
        var response = await this.client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }

    /// <summary>
    /// Test updating a tenant with JSON Patch.
    /// </summary>
    [TestMethod]
    public async Task UpdateTenantWithJsonPatch_ShouldReturnOk()
    {
        // Arrange
        var tenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var jsonPatch = """
            [
                { "op": "replace", "path": "/properties/description", "value": "Updated description" }
            ]
            """;

        // Act
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/{tenantId}/marain/tenant")
        {
            Content = JsonContent.Create(JsonSerializer.Deserialize<object[]>(jsonPatch), 
                mediaType: System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json-patch+json"))
        };
        var response = await this.client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updatedTenant = await response.Content.ReadFromJsonAsync<TenantResponse>();
        updatedTenant.ShouldNotBeNull();
        updatedTenant.Id.ShouldBe(tenantId);
    }

    /// <summary>
    /// Test updating a tenant with invalid content type returns UnsupportedMediaType.
    /// </summary>
    [TestMethod]
    public async Task UpdateTenantWithInvalidContentType_ShouldReturnUnsupportedMediaType()
    {
        // Arrange
        var tenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var jsonPatch = """[{ "op": "replace", "path": "/name", "value": "Updated" }]""";

        // Act
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/{tenantId}/marain/tenant")
        {
            Content = new StringContent(jsonPatch, System.Text.Encoding.UTF8, "application/json") // Wrong content type
        };
        var response = await this.client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    /// <summary>
    /// Test validation errors return BadRequest.
    /// </summary>
    [TestMethod]
    public async Task CreateChildTenantWithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        // Missing tenantName parameter

        // Act
        var response = await this.client.PostAsync($"/{parentTenantId}/marain/tenant", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Test deleting a child tenant that doesn't exist returns NotFound.
    /// </summary>
    [TestMethod]
    public async Task DeleteNonExistentChildTenant_ShouldReturnNotFound()
    {
        // Arrange
        var parentTenantId = "f26450ab-1818-4b64-8c06-ed47a31e0d8e";
        var childTenantId = "nonexistent-child-tenant";

        // Act
        var response = await this.client.DeleteAsync($"/{parentTenantId}/marain/tenant/children/{childTenantId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        this.client.Dispose();
        this.factory.Dispose();
    }
}