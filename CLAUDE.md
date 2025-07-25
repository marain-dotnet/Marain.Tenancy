# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Marain.Tenancy is a .NET 8.0 multi-tenant API service that provides tenant management functionality. The project is built on Azure infrastructure and follows a microservices architecture with both Azure Functions and ASP.NET Core hosting options.

## Solution Structure

The solution is organized into several key projects:

- **Core Services**: 
  - `Marain.Tenancy.OpenApi.Service` - Core tenancy API service with OpenAPI definition
  - `Marain.Tenancy.Client` - Client SDK for consuming the tenancy API
  - `Marain.Tenancy.ClientTenantProvider` - Client-side tenant provider implementation

- **Hosting**: 
  - `Marain.Tenancy.Host.Functions` - Azure Functions hosting for serverless deployment
  - `Marain.Tenancy.Host.AspNetCore` - ASP.NET Core hosting for traditional web hosting
  - `Marain.Tenancy.Hosting.AspNetCore` - ASP.NET Core hosting extensions

- **Storage**: 
  - `Marain.Tenancy.Storage.Azure.BlobStorage` - Azure Blob Storage implementation for tenant data

- **Testing**: 
  - `Marain.Tenancy.Specs` - Integration tests using Reqnroll (formerly SpecFlow)
  - `Marain.Tenancy.Storage.Azure.BlobStorage.Specs` - Storage-specific tests

- **Tools**: 
  - `Marain.Tenancy.Cli` - Command-line interface for tenant management
  - `Marain.Tenancy.Deployment` - Azure deployment templates and scripts

## Build and Development Commands

### Building the Solution
```bash
dotnet build Solutions/Marain.Tenancy.sln
```

### Running Tests
The project uses Reqnroll (BDD testing framework) with NUnit:
```bash
dotnet test Solutions/Marain.Tenancy.Specs/Marain.Tenancy.Specs.csproj
dotnet test Solutions/Marain.Tenancy.Storage.Azure.BlobStorage.Specs/Marain.Tenancy.Storage.Azure.BlobStorage.Specs.csproj
```

### Running a Single Test Project
```bash
dotnet test Solutions/Marain.Tenancy.Specs/Marain.Tenancy.Specs.csproj --filter "TestCategory=YourCategory"
```

### Package Restore
```bash
dotnet restore Solutions/Marain.Tenancy.sln
```

### Running the Functions Host Locally
```bash
cd Solutions/Marain.Tenancy.Host.Functions
func start
```

### Running the ASP.NET Core Host Locally
```bash
cd Solutions/Marain.Tenancy.Host.AspNetCore
dotnet run
```

## Architecture Notes

### Multi-Host Testing
The test projects use a multi-host testing approach allowing tests to run against different hosting models (Functions vs ASP.NET Core). Look for `IMultiModeTest` and `MultiTestHostBase` classes.

### Dependency Injection
The project uses Microsoft.Extensions.DependencyInjection extensively. Service registration extensions are typically found in `Microsoft.Extensions.DependencyInjection` namespaces within each project.

### Configuration
- Azure Functions use `local.settings.json` for local development
- ASP.NET Core uses `appsettings.json` and `appsettings.template.json`
- Test projects have their own `local.settings.template.json` files

### Storage Abstraction
The tenancy service abstracts storage through interfaces, with Azure Blob Storage as the primary implementation. The storage layer is designed to be pluggable.

### Package Lock Files
Test projects use package lock files (`packages.lock.json`) to ensure repeatability in CI builds. When adding dependencies to test projects, the lock files may need to be updated.

## Testing Framework

The project migrated from SpecFlow to Reqnroll. Test configuration is in `reqnroll.json` files. Tests use Gherkin syntax (.feature files) with step definitions in C#.

## Deployment

Azure deployment is handled through:
- ARM templates in the `Marain.Tenancy.Deployment` project
- Azure DevOps pipelines (`azure-pipelines.yml`)
- PowerShell deployment scripts

## Code Standards

The project follows StyleCop rules configured in `stylecop.json`. Documentation rules require company name "Endjin Limited" in file headers.