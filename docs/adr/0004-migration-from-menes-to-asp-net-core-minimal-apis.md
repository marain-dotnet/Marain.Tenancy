# Migration from Menes to ASP.NET Core Minimal APIs

## Background

Marain APIs were initially built to be hosted in Azure Functions. This was done to provide a low cost approach for hosting via the serverless functions model. At the time, support for API development was lacking in Azure Functions, and as a result the Menes framework was adopted. This provides a contract-first library for mapping endpoint URLs to their corresponding code, validation of both incoming and outgoing requests, and serialization to and from model types.

The Azure platform has moved on significantly since the APIs were first built, with containerisation now being an excellent option for packaging and deploying code to one of the various Azure services that support them. The original arguments for exclusively targetting Azure Functions are no longer valid.

As such we can simplify the Marain service codebases by moving away from the Menes platform and refactoring the code to run as an ASP.NET Core application using the Minimal APIs approach.

## Decision

We will migrate the Marain.Tenancy solution from the Menes framework to ASP.NET Core Minimal APIs to achieve the following benefits:

- **Reduced Complexity**: Eliminate the Menes abstraction layer and use native .NET capabilities
- **Improved Performance**: Leverage the 15-20% performance improvement offered by Minimal APIs over traditional controllers
- **Better Maintainability**: Align with modern .NET development practices and reduce dependency complexity
- **Enhanced Developer Experience**: Use familiar ASP.NET Core patterns and tooling
- **Simplified Hosting**: Remove the need for framework-specific hosting abstractions

## Approach

For Marain.Tenancy, this migration will be implemented in the following phases:

### Phase 1: Foundation Preparation
1. **Create new project structure** for Minimal APIs organization
2. **Define request/response models** to replace HAL-based responses
3. **Implement base service registration** and configuration extensions
4. **Set up OpenAPI documentation** using native ASP.NET Core support
5. **Establish validation framework** using FluentValidation or MiniValidation

### Phase 2: Core API Migration
1. **Convert TenancyService operations** to Minimal API endpoints:
   - `GET /{tenantId}/marain/tenant` (getTenant)
   - `PATCH /{tenantId}/marain/tenant` (updateTenant)
   - `GET /{tenantId}/marain/tenant/children` (getChildren)
   - `POST /{tenantId}/marain/tenant/children` (createChildTenant)
   - `DELETE /{tenantId}/marain/tenant/children/{childTenantId}` (deleteChildTenant)

2. **Replace mappers** to work without HAL dependencies while maintaining link generation
3. **Implement exception handling** using IExceptionHandler for consistent error responses
4. **Add request validation** using endpoint filters

### Phase 3: Hosting Migration
1. **Update ASP.NET Core hosting** to use native Program.cs with Minimal APIs
2. **Migrate Azure Functions hosting** to work with the new endpoint structure
3. **Remove Menes hosting abstractions** (`IOpenApiHost`, `IOpenApiContext`)
4. **Update service registration** to remove Menes dependencies

### Phase 4: Testing Infrastructure Migration
1. **Replace multi-host testing** with WebApplicationFactory-based integration tests
2. **Convert existing Reqnroll tests** to work with the new API structure
3. **Implement unit tests** for individual endpoint methods
4. **Create test utilities** for common testing scenarios
5. **Ensure test coverage** is maintained or improved

### Phase 5: Validation and Optimization
1. **Implement comprehensive validation** for all endpoints
2. **Add response caching** where appropriate
3. **Optimize performance** using Minimal API best practices
4. **Update OpenAPI documentation** for completeness and accuracy

## Implementation Details

### API Structure
The new structure will follow feature-based organization:

```
Endpoints/
├── TenantEndpoints.cs          # Core tenant operations
├── ChildTenantEndpoints.cs     # Child tenant management
└── Extensions/
    └── EndpointExtensions.cs   # Common endpoint utilities
```

### Response Format
Maintain backward compatibility by preserving the existing JSON structure including HAL-style links:

```json
{
  "id": "tenant-id",
  "name": "Tenant Name",
  "contentType": "application/vnd.marain.tenancy.tenant",
  "properties": {},
  "_links": {
    "self": { "href": "/tenant-id/marain/tenant" },
    "children": { "href": "/tenant-id/marain/tenant/children" }
  }
}
```

### Testing Strategy
- **Integration Tests**: Use WebApplicationFactory with in-memory database
- **Unit Tests**: Test individual endpoint methods with mocked dependencies
- **Contract Tests**: Ensure API contract compatibility during migration
- **Performance Tests**: Validate performance improvements

## Risks and Mitigation

### High Risks
1. **API Contract Changes**: Risk of breaking existing clients
   - *Mitigation*: Maintain exact response format compatibility
   - *Validation*: Comprehensive contract testing

2. **Testing Coverage Loss**: Risk of reduced test coverage during migration
   - *Mitigation*: Parallel testing approach during transition
   - *Validation*: Coverage metrics monitoring

### Medium Risks
1. **Performance Regression**: Risk of unexpected performance issues
   - *Mitigation*: Continuous performance monitoring
   - *Validation*: Benchmark comparisons

2. **Link Generation**: Risk of broken hypermedia links
   - *Mitigation*: Create compatibility layer using LinkGenerator
   - *Validation*: Link validation tests

## Success Criteria

### Functional
- [ ] All existing API endpoints function identically
- [ ] Response formats maintain backward compatibility
- [ ] Authentication and authorization work unchanged
- [ ] Error handling behavior is preserved

### Non-Functional
- [ ] Removal of all Menes dependencies
- [ ] Simplified codebase with improved maintainability
