# ASP.NET Core Minimal API Testing with Reqnroll: Comprehensive Guide (2025)

## Table of Contents
1. [Introduction & Overview](#introduction--overview)
2. [WebApplicationFactory Fundamentals](#webapplicationfactory-fundamentals)
3. [Reqnroll Integration](#reqnroll-integration)
4. [Dependency Injection & Mocking](#dependency-injection--mocking)
5. [Integration Testing Patterns](#integration-testing-patterns)
6. [Unit vs Integration Testing Strategies](#unit-vs-integration-testing-strategies)
7. [Practical Examples](#practical-examples)
8. [Best Practices Summary](#best-practices-summary)

## Introduction & Overview

This guide presents the most current best practices for testing ASP.NET Core Minimal API applications using Reqnroll (the successor to SpecFlow) for Behavior-Driven Development (BDD) integration testing as of 2025.

### Key Technologies Covered
- **ASP.NET Core Minimal APIs** (.NET 8+)
- **Reqnroll** - Open-source BDD framework for .NET
- **WebApplicationFactory** - Integration testing framework
- **Microsoft.AspNetCore.Mvc.Testing** - Testing utilities
- **Test frameworks**: NUnit, xUnit, MSTest

### Why This Combination Matters

Minimal APIs in .NET 6+ are excellent for reducing boilerplate code, but they present unique testing challenges:
- **No controller classes** - Traditional controller-based unit testing is not possible
- **Lambda-based endpoints** - Harder to unit test directly
- **Integration testing becomes critical** - WebApplicationFactory fills the gap

Reqnroll provides the BDD layer that bridges business requirements with technical implementation through executable specifications using Gherkin syntax.

## WebApplicationFactory Fundamentals

### Core Concept

`WebApplicationFactory<TEntryPoint>` creates a TestServer for integration tests, where `TEntryPoint` is the entry point class of the System Under Test (SUT), typically `Program.cs`.

### Essential Setup

#### 1. Test Project Configuration

Your test project must use the Web SDK:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    <PackageReference Include="Reqnroll.NUnit" Version="2.0.0" />
    <PackageReference Include="NUnit" Version="4.0.1" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../YourMinimalApi/YourMinimalApi.csproj" />
  </ItemGroup>
</Project>
```

#### 2. Program.cs Accessibility

Make your `Program` class accessible to tests by adding this line at the end of `Program.cs`:

```csharp
public partial class Program { }
```

#### 3. Basic WebApplicationFactory Usage

```csharp
public class BasicMinimalApiTests
{
    [Test]
    public async Task GetProducts_ReturnsSuccessStatusCode()
    {
        // Arrange
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        // Additional assertions...
    }
}
```

### Advanced WebApplicationFactory Patterns

#### Custom WebApplicationFactory

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove production services
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            // Add test services
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
            
            // Override other services as needed
            services.Replace(ServiceDescriptor.Scoped<IExternalService, MockExternalService>());
        });

        builder.UseEnvironment("Testing");
    }
}
```

## Reqnroll Integration

### Installation and Setup

#### NuGet Packages

```xml
<PackageReference Include="Reqnroll.NUnit" Version="2.0.0" />
<PackageReference Include="Reqnroll.Tools.MsBuild.Generation" Version="2.0.0" />
```

#### Reqnroll Configuration (reqnroll.json)

```json
{
  "$schema": "https://schemas.reqnroll.net/reqnroll-config-latest.json",
  "language": {
    "feature": "en-US"
  },
  "bindingCulture": {
    "name": "en-US"
  },
  "framework": {
    "unitTestFramework": "NUnit"
  },
  "trace": {
    "traceSuccessfulSteps": true,
    "traceTimings": false,
    "minTracedDuration": "0:00:00.1"
  }
}
```

### Feature Files and Gherkin Scenarios

#### Example Feature: Tenant Management API

```gherkin
Feature: Tenant Management API
    As an API consumer
    I want to manage tenants through RESTful endpoints
    So that I can create, retrieve, update, and delete tenant information

Background:
    Given the API is running
    And the database is clean

Scenario: Create a new tenant successfully
    Given I have a valid tenant creation request with name "Test Tenant"
    When I POST the request to "/api/tenants"
    Then the response status should be 201 Created
    And the response should contain the created tenant details
    And the tenant should exist in the database

Scenario: Get tenant by ID
    Given a tenant exists with ID "123" and name "Existing Tenant"
    When I GET "/api/tenants/123"
    Then the response status should be 200 OK
    And the response should contain tenant with name "Existing Tenant"

Scenario: Update tenant properties
    Given a tenant exists with ID "456"
    When I PATCH "/api/tenants/456" with updated name "Updated Tenant"
    Then the response status should be 200 OK
    And the tenant name should be updated in the database

Scenario: Delete tenant
    Given a tenant exists with ID "789"
    When I DELETE "/api/tenants/789"
    Then the response status should be 204 No Content
    And the tenant should not exist in the database

Scenario Outline: Handle invalid requests
    Given I have an invalid tenant request with <field> set to <value>
    When I POST the request to "/api/tenants"
    Then the response status should be 400 Bad Request
    And the response should contain validation errors for <field>

    Examples:
    | field | value |
    | name  | ""    |
    | name  | null  |
```

### Step Definitions with WebApplicationFactory

```csharp
[Binding]
public class TenantApiSteps : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpResponseMessage _response = null!;
    private string _requestBody = null!;

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        
        // Initialize test database if needed
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        _client?.Dispose();
        await _factory.DisposeAsync();
    }

    [Given(@"the API is running")]
    public void GivenTheApiIsRunning()
    {
        // WebApplicationFactory ensures the API is running
        _client.Should().NotBeNull();
    }

    [Given(@"the database is clean")]
    public async Task GivenTheDatabaseIsClean()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    [Given(@"I have a valid tenant creation request with name ""(.*)""")]
    public void GivenIHaveValidTenantCreationRequest(string tenantName)
    {
        var request = new CreateTenantRequest { Name = tenantName };
        _requestBody = JsonSerializer.Serialize(request);
    }

    [When(@"I POST the request to ""(.*)""")]
    public async Task WhenIPostTheRequestTo(string endpoint)
    {
        var content = new StringContent(_requestBody, Encoding.UTF8, "application/json");
        _response = await _client.PostAsync(endpoint, content);
    }

    [When(@"I GET ""(.*)""")]
    public async Task WhenIGet(string endpoint)
    {
        _response = await _client.GetAsync(endpoint);
    }

    [When(@"I PATCH ""(.*)"" with updated name ""(.*)""")]
    public async Task WhenIPatchWithUpdatedName(string endpoint, string newName)
    {
        var patchDoc = new[]
        {
            new { op = "replace", path = "/name", value = newName }
        };
        var content = new StringContent(JsonSerializer.Serialize(patchDoc), Encoding.UTF8, "application/json-patch+json");
        _response = await _client.PatchAsync(endpoint, content);
    }

    [When(@"I DELETE ""(.*)""")]
    public async Task WhenIDelete(string endpoint)
    {
        _response = await _client.DeleteAsync(endpoint);
    }

    [Then(@"the response status should be (\d+) (.*)")]
    public void ThenTheResponseStatusShouldBe(int statusCode, string statusDescription)
    {
        _response.StatusCode.Should().Be((HttpStatusCode)statusCode);
    }

    [Then(@"the response should contain the created tenant details")]
    public async Task ThenTheResponseShouldContainCreatedTenantDetails()
    {
        var content = await _response.Content.ReadAsStringAsync();
        var tenant = JsonSerializer.Deserialize<TenantResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        tenant.Should().NotBeNull();
        tenant!.Id.Should().NotBeNullOrEmpty();
        tenant.Name.Should().NotBeNullOrEmpty();
    }

    [Then(@"the tenant should exist in the database")]
    public async Task ThenTheTenantShouldExistInDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var content = await _response.Content.ReadAsStringAsync();
        var tenant = JsonSerializer.Deserialize<TenantResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        var dbTenant = await context.Tenants.FindAsync(tenant!.Id);
        dbTenant.Should().NotBeNull();
    }
}
```

## Dependency Injection & Mocking

### Service Override Patterns

#### 1. ConfigureTestServices Method

The most common pattern for overriding services in tests:

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove production DbContext
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Add in-memory database
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            });

            // Replace external services with mocks
            services.Replace(ServiceDescriptor.Scoped<IEmailService, MockEmailService>());
            services.Replace(ServiceDescriptor.Singleton<IConfiguration>(
                new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ApiKey"] = "test-key",
                        ["ConnectionStrings:Default"] = "test-connection"
                    })
                    .Build()));
        });
    }
}
```

#### 2. Mock Extensions for Improved Readability

```csharp
public static class ServiceCollectionMockExtensions
{
    public static IServiceCollection Mock<TService>(
        this IServiceCollection services, 
        Action<Mock<TService>>? configureMock = null) 
        where TService : class
    {
        var mock = new Mock<TService>();
        configureMock?.Invoke(mock);
        
        services.Replace(ServiceDescriptor.Singleton(mock.Object));
        return services;
    }

    public static IServiceCollection MockScoped<TService>(
        this IServiceCollection services, 
        Action<Mock<TService>>? configureMock = null) 
        where TService : class
    {
        var mock = new Mock<TService>();
        configureMock?.Invoke(mock);
        
        services.Replace(ServiceDescriptor.Scoped(_ => mock.Object));
        return services;
    }
}
```

Usage in tests:

```csharp
builder.ConfigureTestServices(services =>
{
    services.Mock<IEmailService>(mock =>
    {
        mock.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
    });
    
    services.MockScoped<IPaymentService>(mock =>
    {
        mock.Setup(x => x.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult { Success = true, TransactionId = "test-123" });
    });
});
```

#### 3. Test Data Builder Pattern

```csharp
public class TestDataBuilder
{
    private readonly IServiceCollection _services;
    private readonly Dictionary<string, object> _testData = new();

    public TestDataBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public TestDataBuilder WithTenant(string id, string name)
    {
        _testData[$"tenant_{id}"] = new Tenant { Id = id, Name = name };
        return this;
    }

    public TestDataBuilder WithUser(string id, string email, string tenantId)
    {
        _testData[$"user_{id}"] = new User { Id = id, Email = email, TenantId = tenantId };
        return this;
    }

    public void Apply()
    {
        _services.MockScoped<IDataSeeder>(mock =>
        {
            mock.Setup(x => x.SeedAsync())
                .Returns(Task.CompletedTask)
                .Callback(() =>
                {
                    // Use test data to populate in-memory database
                });
        });
    }
}

// Usage in WebApplicationFactory
builder.ConfigureTestServices(services =>
{
    new TestDataBuilder(services)
        .WithTenant("tenant-1", "Test Tenant")
        .WithUser("user-1", "test@example.com", "tenant-1")
        .Apply();
});
```

## Integration Testing Patterns

### Test Isolation Strategies

#### 1. Database Isolation with Collection Fixtures

```csharp
[Collection("Database Collection")]
public class TenantApiIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _databaseFixture;
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public TenantApiIntegrationTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        
        // Ensure clean database state for each test
        await _databaseFixture.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }
}

[CollectionDefinition("Database Collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

public class DatabaseFixture : IAsyncLifetime
{
    private readonly string _connectionString;

    public DatabaseFixture()
    {
        _connectionString = $"Server=(localdb)\\mssqllocaldb;Database=TestDb_{Guid.NewGuid()};Trusted_Connection=true;";
    }

    public async Task InitializeAsync()
    {
        // Create test database
        using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up test database
        using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new AppDbContext(options);
    }
}
```

#### 2. TestContainers Integration

```csharp
public class ContainerizedWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Test123456!")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove production DbContext
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Add containerized database
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(_msSqlContainer.GetConnectionString());
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();
        
        // Run migrations
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
```

### Performance Optimization

#### 1. Shared Application Instance

```csharp
public class SharedWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static SharedWebApplicationFactory? _instance;
    private static int _referenceCount;

    public static async Task<SharedWebApplicationFactory> GetInstanceAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            if (_instance == null)
            {
                _instance = new SharedWebApplicationFactory();
                await _instance.InitializeAsync();
            }
            _referenceCount++;
            return _instance;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public static async Task ReleaseInstanceAsync()
    {
        await Semaphore.WaitAsync();
        try
        {
            _referenceCount--;
            if (_referenceCount <= 0 && _instance != null)
            {
                await _instance.DisposeAsync();
                _instance = null;
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task InitializeAsync()
    {
        // One-time setup
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureDeletedAsync();
        
        await base.DisposeAsync();
    }
}
```

## Unit vs Integration Testing Strategies

### When to Use Unit Tests

Use unit tests for Minimal API route handlers when:
- Testing business logic within handler methods
- Validating input parsing and validation
- Testing return type behavior
- Mocking dependencies is straightforward

#### Unit Testing with TypedResults

```csharp
// Minimal API endpoint
public static class ProductEndpoints
{
    public static async Task<Results<Ok<ProductResponse>, NotFound, BadRequest<string>>> GetProduct(
        int id, 
        IProductService productService)
    {
        if (id <= 0)
            return TypedResults.BadRequest("Invalid product ID");

        var product = await productService.GetByIdAsync(id);
        if (product == null)
            return TypedResults.NotFound();

        return TypedResults.Ok(new ProductResponse(product));
    }
}

// Unit test
[TestFixture]
public class ProductEndpointsTests
{
    private Mock<IProductService> _productServiceMock = null!;

    [SetUp]
    public void Setup()
    {
        _productServiceMock = new Mock<IProductService>();
    }

    [Test]
    public async Task GetProduct_WithValidId_ReturnsOkResult()
    {
        // Arrange
        var productId = 1;
        var product = new Product { Id = productId, Name = "Test Product" };
        _productServiceMock.Setup(x => x.GetByIdAsync(productId))
                          .ReturnsAsync(product);

        // Act
        var result = await ProductEndpoints.GetProduct(productId, _productServiceMock.Object);

        // Assert
        result.Should().BeOfType<Results<Ok<ProductResponse>, NotFound, BadRequest<string>>>();
        
        // Extract the actual result
        var okResult = result.Result as Ok<ProductResponse>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().NotBeNull();
        okResult.Value!.Name.Should().Be("Test Product");
    }

    [Test]
    public async Task GetProduct_WithInvalidId_ReturnsBadRequest()
    {
        // Arrange
        var invalidId = -1;

        // Act
        var result = await ProductEndpoints.GetProduct(invalidId, _productServiceMock.Object);

        // Assert
        result.Should().BeOfType<Results<Ok<ProductResponse>, NotFound, BadRequest<string>>>();
        
        var badRequestResult = result.Result as BadRequest<string>;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.Value.Should().Be("Invalid product ID");
    }

    [Test]
    public async Task GetProduct_WhenProductNotFound_ReturnsNotFound()
    {
        // Arrange
        var productId = 999;
        _productServiceMock.Setup(x => x.GetByIdAsync(productId))
                          .ReturnsAsync((Product?)null);

        // Act
        var result = await ProductEndpoints.GetProduct(productId, _productServiceMock.Object);

        // Assert
        result.Should().BeOfType<Results<Ok<ProductResponse>, NotFound, BadRequest<string>>>();
        
        var notFoundResult = result.Result as NotFound;
        notFoundResult.Should().NotBeNull();
    }
}
```

### When to Use Integration Tests

Use integration tests for:
- End-to-end request/response validation
- Authentication and authorization flows
- Database interactions
- External service integrations
- Complex business workflows

#### Integration Test Example

```csharp
[TestFixture]
public class ProductApiIntegrationTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }

    [Test]
    public async Task GetProduct_EndToEnd_Success()
    {
        // Arrange - Seed test data
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var product = new Product { Name = "Integration Test Product", Price = 99.99m };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/products/{product.Id}");

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var productResponse = JsonSerializer.Deserialize<ProductResponse>(content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        productResponse.Should().NotBeNull();
        productResponse!.Name.Should().Be("Integration Test Product");
        productResponse.Price.Should().Be(99.99m);
    }

    [Test]
    public async Task CreateProduct_WithValidation_ReturnsBadRequest()
    {
        // Arrange
        var invalidProduct = new CreateProductRequest { Name = "", Price = -1 };
        var content = new StringContent(
            JsonSerializer.Serialize(invalidProduct), 
            Encoding.UTF8, 
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/products", content);

        // Assert
        response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
        
        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("Name is required");
        errorContent.Should().Contain("Price must be greater than 0");
    }
}
```

## Practical Examples

### Complete Reqnroll + WebApplicationFactory Example

#### Feature File: TenantManagement.feature

```gherkin
Feature: Tenant Management API Integration
    As a system administrator
    I want to manage tenants through the API
    So that I can maintain the multi-tenant system

Background:
    Given the tenant management API is running
    And the database is clean

Scenario: Create tenant with all required properties
    Given I have a tenant creation request with the following details:
        | Property    | Value                |
        | Name        | Acme Corporation     |
        | Description | Main tenant for Acme |
        | IsActive    | true                 |
    When I submit the create tenant request
    Then the response should indicate success with status code 201
    And the response should contain the tenant ID
    And the tenant should be stored in the database with correct properties

Scenario: Retrieve tenant hierarchy
    Given the following tenant hierarchy exists:
        | Tenant ID | Parent ID | Name           |
        | root      |           | Root Tenant    |
        | acme      | root      | Acme Corp      |
        | acme-dev  | acme      | Acme Dev Team  |
        | acme-prod | acme      | Acme Prod Team |
    When I request the children of tenant "acme"
    Then the response should contain 2 child tenants
    And the child tenants should be "acme-dev" and "acme-prod"

Scenario: Update tenant properties using JSON Patch
    Given a tenant exists with ID "update-test" and name "Original Name"
    When I send a PATCH request to update the tenant name to "Updated Name"
    Then the response should indicate success with status code 200
    And the tenant name should be "Updated Name" in the database

Scenario: Delete tenant with children should fail
    Given a tenant "parent-tenant" exists with child tenant "child-tenant"
    When I attempt to delete tenant "parent-tenant"
    Then the response should indicate conflict with status code 409
    And the error message should mention that the tenant has children
    And the tenant should still exist in the database

Scenario Outline: Handle various invalid tenant creation requests
    Given I have an invalid tenant creation request with <field> set to <value>
    When I submit the create tenant request
    Then the response should indicate bad request with status code 400
    And the validation error should mention <field>

    Examples:
        | field       | value                    |
        | Name        | ""                      |
        | Name        | null                    |
        | Name        | "a"                     |
        | Description | "x".repeat(1001)        |
```

#### Step Definitions

```csharp
[Binding]
public class TenantManagementSteps : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private HttpResponseMessage? _lastResponse;
    private object? _lastRequestBody;
    private readonly Dictionary<string, object> _testData = new();

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        
        // Ensure clean database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _lastResponse?.Dispose();
        _client?.Dispose();
        await _factory.DisposeAsync();
    }

    [Given(@"the tenant management API is running")]
    public void GivenTheTenantManagementApiIsRunning()
    {
        // WebApplicationFactory ensures API is running
        _client.Should().NotBeNull();
    }

    [Given(@"the database is clean")]
    public async Task GivenTheDatabaseIsClean()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        
        // Clean all tenant data
        context.Tenants.RemoveRange(context.Tenants);
        await context.SaveChangesAsync();
    }

    [Given(@"I have a tenant creation request with the following details:")]
    public void GivenIHaveATenantCreationRequestWithTheFollowingDetails(Table table)
    {
        var request = new CreateTenantRequest();
        
        foreach (var row in table.Rows)
        {
            var property = row["Property"];
            var value = row["Value"];
            
            switch (property)
            {
                case "Name":
                    request.Name = value;
                    break;
                case "Description":
                    request.Description = value;
                    break;
                case "IsActive":
                    request.IsActive = bool.Parse(value);
                    break;
            }
        }
        
        _lastRequestBody = request;
    }

    [Given(@"the following tenant hierarchy exists:")]
    public async Task GivenTheFollowingTenantHierarchyExists(Table table)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        
        foreach (var row in table.Rows)
        {
            var tenant = new Tenant
            {
                Id = row["Tenant ID"],
                ParentId = string.IsNullOrEmpty(row["Parent ID"]) ? null : row["Parent ID"],
                Name = row["Name"],
                CreatedAt = DateTimeOffset.UtcNow
            };
            
            context.Tenants.Add(tenant);
            _testData[$"tenant_{tenant.Id}"] = tenant;
        }
        
        await context.SaveChangesAsync();
    }

    [Given(@"a tenant exists with ID ""(.*)"" and name ""(.*)""")]
    public async Task GivenATenantExistsWithIdAndName(string tenantId, string tenantName)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = tenantName,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        
        _testData[$"tenant_{tenantId}"] = tenant;
    }

    [Given(@"a tenant ""(.*)"" exists with child tenant ""(.*)""")]
    public async Task GivenATenantExistsWithChildTenant(string parentTenantId, string childTenantId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        
        var parentTenant = new Tenant
        {
            Id = parentTenantId,
            Name = $"Parent {parentTenantId}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        var childTenant = new Tenant
        {
            Id = childTenantId,
            ParentId = parentTenantId,
            Name = $"Child {childTenantId}",
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        context.Tenants.AddRange(parentTenant, childTenant);
        await context.SaveChangesAsync();
        
        _testData[$"tenant_{parentTenantId}"] = parentTenant;
        _testData[$"tenant_{childTenantId}"] = childTenant;
    }

    [When(@"I submit the create tenant request")]
    public async Task WhenISubmitTheCreateTenantRequest()
    {
        var json = JsonSerializer.Serialize(_lastRequestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        _lastResponse = await _client.PostAsync("/api/tenants", content);
    }

    [When(@"I request the children of tenant ""(.*)""")]
    public async Task WhenIRequestTheChildrenOfTenant(string tenantId)
    {
        _lastResponse = await _client.GetAsync($"/api/tenants/{tenantId}/children");
    }

    [When(@"I send a PATCH request to update the tenant name to ""(.*)""")]
    public async Task WhenISendAPatchRequestToUpdateTheTenantNameTo(string newName)
    {
        var patchDoc = new[]
        {
            new { op = "replace", path = "/name", value = newName }
        };
        
        var json = JsonSerializer.Serialize(patchDoc);
        var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");
        
        var tenantId = _testData.Keys.First(k => k.StartsWith("tenant_")).Replace("tenant_", "");
        _lastResponse = await _client.PatchAsync($"/api/tenants/{tenantId}", content);
    }

    [When(@"I attempt to delete tenant ""(.*)""")]
    public async Task WhenIAttemptToDeleteTenant(string tenantId)
    {
        _lastResponse = await _client.DeleteAsync($"/api/tenants/{tenantId}");
    }

    [Then(@"the response should indicate success with status code (\d+)")]
    public void ThenTheResponseShouldIndicateSuccessWithStatusCode(int expectedStatusCode)
    {
        _lastResponse.Should().NotBeNull();
        _lastResponse!.StatusCode.Should().Be((HttpStatusCode)expectedStatusCode);
    }

    [Then(@"the response should contain the tenant ID")]
    public async Task ThenTheResponseShouldContainTheTenantId()
    {
        _lastResponse.Should().NotBeNull();
        
        var content = await _lastResponse!.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<TenantResponse>(content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        response.Should().NotBeNull();
        response!.Id.Should().NotBeNullOrEmpty();
        
        _testData["last_created_tenant_id"] = response.Id;
    }

    [Then(@"the tenant should be stored in the database with correct properties")]
    public async Task ThenTheTenantShouldBeStoredInTheDatabaseWithCorrectProperties()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        
        var tenantId = (string)_testData["last_created_tenant_id"];
        var tenant = await context.Tenants.FindAsync(tenantId);
        
        tenant.Should().NotBeNull();
        
        var originalRequest = (CreateTenantRequest)_lastRequestBody!;
        tenant!.Name.Should().Be(originalRequest.Name);
        tenant.Description.Should().Be(originalRequest.Description);
        tenant.IsActive.Should().Be(originalRequest.IsActive);
    }

    [Then(@"the response should contain (\d+) child tenants")]
    public async Task ThenTheResponseShouldContainChildTenants(int expectedCount)
    {
        _lastResponse.Should().NotBeNull();
        
        var content = await _lastResponse!.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<ChildTenantsResponse>(content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        response.Should().NotBeNull();
        response!.Tenants.Should().HaveCount(expectedCount);
    }

    [Then(@"the child tenants should be ""(.*)"" and ""(.*)""")]
    public async Task ThenTheChildTenantsShouldBeAnd(string expectedChild1, string expectedChild2)
    {
        _lastResponse.Should().NotBeNull();
        
        var content = await _lastResponse!.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<ChildTenantsResponse>(content, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        response.Should().NotBeNull();
        var tenantIds = response!.Tenants.Select(t => t.Id).ToList();
        
        tenantIds.Should().Contain(expectedChild1);
        tenantIds.Should().Contain(expectedChild2);
    }

    [Then(@"the tenant name should be ""(.*)"" in the database")]
    public async Task ThenTheTenantNameShouldBeInTheDatabase(string expectedName)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        
        var tenantId = _testData.Keys.First(k => k.StartsWith("tenant_")).Replace("tenant_", "");
        var tenant = await context.Tenants.FindAsync(tenantId);
        
        tenant.Should().NotBeNull();
        tenant!.Name.Should().Be(expectedName);
    }

    [Then(@"the response should indicate conflict with status code (\d+)")]
    public void ThenTheResponseShouldIndicateConflictWithStatusCode(int expectedStatusCode)
    {
        _lastResponse.Should().NotBeNull();
        _lastResponse!.StatusCode.Should().Be((HttpStatusCode)expectedStatusCode);
    }

    [Then(@"the error message should mention that the tenant has children")]
    public async Task ThenTheErrorMessageShouldMentionThatTheTenantHasChildren()
    {
        _lastResponse.Should().NotBeNull();
        
        var content = await _lastResponse!.Content.ReadAsStringAsync();
        content.Should().Contain("children", "Cannot delete tenant with children");
    }

    [Then(@"the tenant should still exist in the database")]
    public async Task ThenTheTenantShouldStillExistInTheDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        
        var parentTenantId = _testData.Keys
            .Where(k => k.StartsWith("tenant_"))
            .Select(k => k.Replace("tenant_", ""))
            .First(id => !id.Contains("child"));
            
        var tenant = await context.Tenants.FindAsync(parentTenantId);
        tenant.Should().NotBeNull();
    }

    [Then(@"the response should indicate bad request with status code (\d+)")]
    public void ThenTheResponseShouldIndicateBadRequestWithStatusCode(int expectedStatusCode)
    {
        _lastResponse.Should().NotBeNull();
        _lastResponse!.StatusCode.Should().Be((HttpStatusCode)expectedStatusCode);
    }

    [Then(@"the validation error should mention (.*)")]
    public async Task ThenTheValidationErrorShouldMention(string fieldName)
    {
        _lastResponse.Should().NotBeNull();
        
        var content = await _lastResponse!.Content.ReadAsStringAsync();
        content.Should().ContainAnyOf(fieldName, fieldName.ToLower(), fieldName.ToUpper());
    }
}
```

#### Custom WebApplicationFactory for Tests

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDatabaseName = "TenancyTestDb";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove production database
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TenancyDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Add test database
            services.AddDbContext<TenancyDbContext>(options =>
            {
                options.UseInMemoryDatabase(TestDatabaseName + Guid.NewGuid());
                options.EnableSensitiveDataLogging();
            });

            // Override external services
            services.Replace(ServiceDescriptor.Scoped<INotificationService, MockNotificationService>());
            services.Replace(ServiceDescriptor.Singleton<IDateTimeProvider, TestDateTimeProvider>());

            // Configure test-specific settings
            services.Configure<TenancyOptions>(options =>
            {
                options.MaxTenantDepth = 5;
                options.EnableAuditLogging = false;
            });
        });

        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });
    }

    public async Task InitializeAsync()
    {
        // Any one-time setup
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        // Cleanup
        await base.DisposeAsync();
    }
}

// Mock services for testing
public class MockNotificationService : INotificationService
{
    private readonly List<NotificationMessage> _sentMessages = new();

    public Task SendAsync(NotificationMessage message)
    {
        _sentMessages.Add(message);
        return Task.CompletedTask;
    }

    public IReadOnlyList<NotificationMessage> SentMessages => _sentMessages.AsReadOnly();
}

public class TestDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
```

## Best Practices Summary

### 1. Test Organization

- **Separate concerns**: Keep unit tests and integration tests in different projects
- **Use descriptive names**: Test names should clearly describe the scenario
- **Group related tests**: Use test collections and fixtures appropriately
- **Follow AAA pattern**: Arrange, Act, Assert

### 2. WebApplicationFactory Usage

- **One factory per test class**: Create a new factory instance for each test class
- **Use ConfigureTestServices**: Override dependencies consistently
- **Implement IAsyncLifetime**: Proper async setup and teardown
- **Environment isolation**: Use test-specific configurations

### 3. Database Testing

- **Clean state**: Ensure each test starts with a clean database
- **Test isolation**: Prevent tests from interfering with each other
- **Use appropriate tools**: In-memory for fast tests, containers for realism
- **Seed data carefully**: Only create necessary test data

### 4. Dependency Management

- **Mock external services**: Don't test third-party integrations
- **Use service overrides**: Replace production services with test doubles
- **Configure test settings**: Use test-specific configuration values
- **Manage test data**: Use builders and factories for complex test data

### 5. Reqnroll Best Practices

- **Business-focused scenarios**: Write scenarios from user perspective
- **Reusable step definitions**: Create parameterized, reusable steps
- **Background steps**: Use for common setup across scenarios
- **Data tables**: Use for parameterized testing
- **Descriptive feature files**: Clear, understandable Gherkin

### 6. Performance Considerations

- **Limit integration tests**: Focus on critical paths
- **Parallel execution**: Enable parallel test execution where possible
- **Shared resources**: Use collection fixtures for expensive setup
- **Fast feedback**: Keep test execution time reasonable

### 7. Maintenance and Evolution

- **Version compatibility**: Keep testing dependencies up to date
- **Refactor regularly**: Remove duplicate test code
- **Document patterns**: Maintain consistent testing patterns
- **CI/CD integration**: Ensure tests run reliably in pipelines

This comprehensive guide provides the foundation for implementing robust, maintainable tests for ASP.NET Core Minimal API applications using Reqnroll and modern testing practices as of 2025.