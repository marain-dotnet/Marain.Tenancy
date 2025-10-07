# Menes Library Research

## Overview

Menes is an open-source .NET library that provides abstractions for building OpenAPI-based web services. It's developed by endjin Limited, a UK-based Microsoft Gold Partner, and released under the Apache 2.0 license.

## Key Information

- **Target Framework**: netstandard2.1 (netstandard2.0 in v2.x)
- **License**: Apache 2.0
- **Repository**: https://github.com/menes-dotnet/Menes
- **Primary Purpose**: Provides abstractions for the Menes framework to build OpenAPI-compliant web services

## Core Components and Namespaces

Based on analysis of the Marain.Tenancy codebase, Menes provides several key namespaces:

### Core Abstractions
- `Menes` - Core framework interfaces and types
- `Menes.Abstractions` - Base abstractions (referenced as NuGet package v6.0.3)

### Service Implementation
- `IOpenApiService` - Interface for implementing OpenAPI services
- `[EmbeddedOpenApiDefinition]` - Attribute for embedding OpenAPI specifications

### HAL (Hypertext Application Language) Support
- `Menes.Hal` - HAL-specific implementations for hypermedia APIs
- `Menes.Links` - Link generation and management for RESTful APIs

### Hosting Support
- `Menes.Hosting.AspNetCore` - ASP.NET Core integration
- `IOpenApiHostConfiguration` - Configuration interface for hosting setup

### Exception Handling
- `Menes.Exceptions` - Framework-specific exception types

### Testing Support
- `Menes.Testing.AspNetCoreSelfHosting` - Testing utilities for self-hosted scenarios

## Usage in Marain.Tenancy

### 1. Service Implementation
The `TenancyService` class implements `IOpenApiService` and uses the `[EmbeddedOpenApiDefinition]` attribute to specify its OpenAPI specification file:

```csharp
[EmbeddedOpenApiDefinition("Marain.Tenancy.OpenApi.TenancyServices.yaml")]
public class TenancyService : IOpenApiService
```

### 2. HAL-based API Responses
The service uses HAL (Hypertext Application Language) for hypermedia-driven API responses, providing self-describing APIs with embedded links.

### 3. OpenAPI Specification
The service includes an embedded OpenAPI 3.0 specification (`TenancyServices.yaml`) that defines:
- RESTful endpoints for tenant management
- HTTP methods (GET, PATCH, POST, DELETE)
- Request/response schemas
- Authentication and authorization requirements

### 4. Hosting Integration
Menes integrates with both:
- **Azure Functions**: Via `Menes.Hosting.AspNetCore` for serverless deployment
- **ASP.NET Core**: For traditional web hosting scenarios

### 5. Dependency Injection
The framework provides extension methods for service registration:
- `AddTenancyApi()` - Legacy method
- `AddTenancyApiWithOpenApiActionResultHosting()` - Current approach
- `AddTenancyApiWithAspNetPipelineHosting()` - Alternative hosting model

## Key Features Observed

### 1. **Code-First OpenAPI**
Services are defined as C# classes with attributes, and the OpenAPI specification is embedded as a resource.

### 2. **HAL Compliance**
Built-in support for HAL (Hypertext Application Language) for creating hypermedia-driven REST APIs.

### 3. **Multi-Host Support**
Designed to work across different hosting models (Azure Functions, ASP.NET Core) with consistent behavior.

### 4. **Testing Framework**
Includes testing utilities for integration testing across different hosting scenarios.

### 5. **Link Generation**
Automatic link generation for RESTful resource relationships.

## Architecture Benefits

1. **Consistency**: Ensures consistent API behavior across different hosting environments
2. **Hypermedia**: HAL support enables self-describing APIs with embedded navigation
3. **Type Safety**: Strong typing for OpenAPI specifications and request/response models
4. **Testability**: Built-in testing support for multi-host scenarios
5. **Standards Compliance**: Full OpenAPI 3.0 specification support

## Project Quality

The Menes project follows endjin's IP Maturity Matrix (IMM) which tracks quality across multiple dimensions:
- Shared Engineering Standards
- Coding Standards  
- Executable Specifications
- Code Coverage
- Reference Documentation
- Benchmarks

## Commercial Support

Commercial support is available through endjin Limited for enterprise scenarios.

## Conclusion

Menes provides a robust framework for building OpenAPI-compliant web services in .NET with strong emphasis on hypermedia (HAL), multi-host deployment, and testing. It abstracts away much of the complexity of building RESTful APIs while maintaining standards compliance and enabling flexible deployment scenarios.