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

### Running the MinimalApi Service Locally
The MinimalApi provides a lightweight ASP.NET Core implementation of the tenancy service:

```bash
# Quick start using the run script
./run-minimalapi.sh

# Or manually navigate and run
cd Solutions/Marain.Tenancy.MinimalApi
dotnet run

# Run with specific profile
dotnet run --launch-profile http   # HTTP only (port 5138)
dotnet run --launch-profile https  # HTTP + HTTPS (ports 5138, 7124)
```

**Access Points:**
- **HTTP**: http://localhost:5138
- **HTTPS**: https://localhost:7124
- **Swagger JSON**: http://localhost:5138/swagger
- **Swagger UI**: http://localhost:5138/swagger-ui
- **Health Check**: http://localhost:5138/health

**Dev Container Access:**
When running in the dev container, the MinimalApi is accessible from the host machine on the same ports thanks to Docker port forwarding configured in `docker-compose.yml`.

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

The project follows StyleCop rules configured in `stylecop.json` and `.editorconfig`. Documentation rules require company name "Endjin Limited" in file headers.

### StyleCop Compliance

The solution uses `Endjin.RecommendedPractices.GitHub` which includes StyleCop.Analyzers and Roslynator.Analyzers for comprehensive code quality enforcement.

#### Key StyleCop Requirements:

1. **File Headers**: All C# files must include copyright headers:
   ```csharp
   // <copyright file="FileName.cs" company="Endjin Limited">
   // Copyright (c) Endjin Limited. All rights reserved.
   // </copyright>
   ```

2. **XML Documentation**: Public members require XML documentation:
   ```csharp
   /// <summary>
   /// Description of the class, method, or property.
   /// </summary>
   /// <param name="parameterName">Description of the parameter.</param>
   /// <returns>Description of the return value.</returns>
   ```

3. **File-Scoped Namespaces**: Use file-scoped namespace declarations:
   ```csharp
   namespace MyProject.MyNamespace;
   
   // Class content follows
   ```

4. **Using Directives**: Place using directives inside the namespace (as configured in .editorconfig):
   ```csharp
   namespace MyProject.MyNamespace;
   
   using System;
   using Microsoft.Extensions.DependencyInjection;
   ```

5. **this Qualifier**: Use this prefix for field, property, method, and event access where required by SA1101.

#### Disabled Rules:
- `SA1025`: Multiple whitespace characters allowed (for switch expressions)
- `SA1642`: Constructor summaries don't require standard boilerplate text
- `SA1600`: Documentation requirements are disabled via `/nowarn:SA1600`
- `SA1122`: Empty string literals don't require string.Empty

#### Code Quality Commands:

```bash
# Build and check for StyleCop violations
dotnet build --verbosity normal | grep -E "(SA|CA|CS)[0-9]{4}"

# Clean build with full analyzer output
dotnet clean && dotnet build --verbosity detailed

# Check specific project for violations
dotnet build ProjectName.csproj --verbosity quiet | grep -E "(warning|error|SA|CA)"

# Remove trailing whitespace from all C# files
find . -name "*.cs" -exec sed -i 's/[[:space:]]*$//' {} \;

# Check for trailing whitespace using sed
sed -n 'l' filename.cs | grep -n '\$'
```

#### Specific StyleCop Rules to Follow:

**SA1503: Braces must not be omitted from multi-line child statement**
```csharp
// ❌ Wrong - missing braces
if (condition)
    DoSomething();

// ✅ Correct - braces required
if (condition)
{
    DoSomething();
}
```

**SA1028: Code must not contain trailing whitespace**
- Ensure no trailing spaces at the end of lines
- Configure your editor to show/remove trailing whitespace

**SA1101: Prefix local calls with this**
```csharp
// ❌ Wrong - missing this prefix
public class MyClass
{
    private string name;
    
    public void SetName(string value)
    {
        name = value; // Missing this
    }
}

// ✅ Correct - use this prefix
public class MyClass
{
    private string name;
    
    public void SetName(string value)
    {
        this.name = value;
    }
}
```

**SA1206: Declaration keywords must follow order**
```csharp
// ❌ Wrong order
readonly private static string Value;

// ✅ Correct order: access modifier, static, readonly
private static readonly string Value;
```

**SA1413: Use trailing comma in multi-line initializers**
```csharp
// ❌ Wrong - missing trailing comma
var items = new[]
{
    "item1",
    "item2",
    "item3"  // Missing comma
};

// ✅ Correct - trailing comma required
var items = new[]
{
    "item1",
    "item2",
    "item3", // Trailing comma
};P
```

**SA1512: Single-line comments should not be followed by blank line**
```csharp
// ❌ Wrong - blank line after single-line comment
// Add environment variables configuration

builder.Configuration.AddEnvironmentVariables();

// ✅ Correct - no blank line after single-line comment
// Add environment variables configuration
builder.Configuration.AddEnvironmentVariables();
```

**IDE0007: Use explicit type instead of 'var'**
```csharp
// ❌ Wrong - using var
var response = new TenantResponse();
var validationResult = await validator.ValidateAsync(model);

// ✅ Correct - explicit types
TenantResponse response = new TenantResponse();
FluentValidation.Results.ValidationResult validationResult = await validator.ValidateAsync(model);
```

#### StyleCop Violation Detection Tools:

**Comprehensive Trailing Whitespace Detection:**
```bash
# Remove all trailing whitespace from C# files
find . -name "*.cs" -exec sed -i 's/[[:space:]]*$//' {} \;

# Check for trailing whitespace using sed (shows line endings)
sed -n 'l' filename.cs | grep -n '\$'

# Use ripgrep to find trailing whitespace
rg " $" --type cs
```

**Multi-line Initializer Detection:**
```bash
# Find object/array initializers that may need trailing commas
rg -U "{\s*\n.*\n.*[^,]\s*\n\s*}" --type cs
```

#### Lessons Learned - StyleCop Compliance:

**Why Previous Scans Missed Violations:**

1. **Inadequate Detection Tools**: Visual inspection is insufficient for trailing whitespace. Always use systematic tools like `sed`, `grep`, or editor features.

2. **Incomplete Pattern Matching**: Focused only on arrays for SA1413 but missed object initializers. Must check all multi-line initializers including:
   - Object initializers: `new MyClass { Prop1 = value, Prop2 = value, }`
   - Array initializers: `new[] { "item1", "item2", }`
   - Dictionary initializers: `new Dictionary<string, object> { ["key"] = value, }`

3. **Scope Limitations**: Only checked primary files but missed comprehensive scanning across all file types and locations.

**Systematic Approach Required:**
- Use automated tools like `find` + `sed` for whitespace
- Use `rg` or `grep` with proper regex patterns  
- Check build output with `grep -E "(SA|IDE)[0-9]{4}"`
- Verify fixes with actual build before claiming completion

#### Best Practices for New Code:
- Always include proper copyright headers in new files
- Document all public APIs with XML comments
- Use file-scoped namespaces
- Follow existing code patterns in the solution
- Run build validation before committing changes
- Use ArgumentNullException.ThrowIfNull() for parameter validation
- Always use braces for control statements, even single-line ones
- Add trailing commas in multi-line initializers
- Prefix instance member access with 'this'
- Follow proper declaration keyword order
- Use explicit types instead of 'var' for clarity
- Remove all trailing whitespace systematically