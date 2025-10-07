# Azure Storage Emulator Setup Strategy for Local Development

## Executive Summary

This document outlines the comprehensive strategy for running Azure Storage Emulator (Azurite) locally while addressing port conflicts, ease of setup, and developer experience. After extensive research and analysis, we recommend a **Hybrid Approach** combining Testcontainers .NET integration for isolated integration tests with DevContainer Docker Compose integration for development convenience.

## Problem Statement

The Marain.Tenancy.Storage.Azure.BlobStorage.Specs project tests were failing because:

1. **Missing Configuration**: No `local.settings.json` file (only template existed)
2. **Azure Storage Emulator Unavailable**: Tests attempted to connect to `127.0.0.1:10000` but no emulator was running
3. **Port Management Challenges**: Need to avoid conflicts when multiple developers or CI pipelines run tests
4. **Setup Complexity**: Developers should be able to clone and run without additional setup steps

## Research Findings

### Current Azure Storage Emulator Landscape (2024)

#### Traditional Azure Storage Emulator (Deprecated)
- **Status**: Microsoft deprecated the Windows-only Azure Storage Emulator
- **Ports**: Fixed ports 10000, 10001, 10002
- **Limitations**: Windows-only, being phased out
- **Recommendation**: Do not use for new projects

#### Azurite (Modern Replacement)
- **Status**: Official cross-platform replacement
- **Ports**: Configurable (default: 10000 blob, 10001 queue, 10002 table)
- **Platform**: Cross-platform Node.js application
- **Deployment**: Available as NPM package, Docker image, or standalone binary
- **Docker Image**: `mcr.microsoft.com/azure-storage/azurite`

### Evaluated Approaches

#### 1. Testcontainers .NET with Azurite (⭐ PRIMARY RECOMMENDATION)

**Overview**: Use Testcontainers library to programmatically manage Azurite Docker containers during test execution.

**Pros**:
- ✅ **Automatic Port Management**: Testcontainers allocates random available ports (e.g., 59273, 59274, 59275)
- ✅ **Zero Manual Setup**: Developers just run tests - no additional installation required
- ✅ **Test Isolation**: Each test class/run gets fresh Azurite instance
- ✅ **Parallel Test Support**: Multiple test runs can execute simultaneously without conflicts
- ✅ **Automatic Cleanup**: Containers automatically destroyed after tests
- ✅ **CI/CD Compatible**: Works in GitHub Actions, Azure DevOps, etc.

**Cons**:
- ⚠️ **Docker Dependency**: Requires Docker (already available in devcontainer)
- ⚠️ **Startup Overhead**: ~2-3 seconds per test class (cached for class lifetime)

**Implementation Details**:
```csharp
// Example usage
var azuriteContainer = new AzuriteBuilder()
    .WithImage("mcr.microsoft.com/azure-storage/azurite")
    .Build();

await azuriteContainer.StartAsync();

string connectionString = azuriteContainer.GetConnectionString();
// Use connectionString for Azure Storage SDK
```

**Port Allocation Example**:
- Container starts with random external ports
- Testcontainers provides `container.GetMappedPublicPort(10000)` → returns random port like `59273`
- Connection string: `DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;...BlobEndpoint=http://127.0.0.1:59273/devstoreaccount1`

#### 2. DevContainer Docker Compose Integration (⭐ SECONDARY RECOMMENDATION)

**Overview**: Add Azurite service to existing Docker Compose configuration in devcontainer.

**Pros**:
- ✅ **Always Available**: Azurite starts with devcontainer, available for debugging
- ✅ **Shared State**: Useful for development scenarios where persistence is helpful
- ✅ **Zero Additional Steps**: Starts automatically with devcontainer
- ✅ **Familiar Setup**: Extends existing Docker Compose infrastructure

**Cons**:
- ⚠️ **Shared State**: Tests may interfere with each other
- ⚠️ **Port Conflicts**: Fixed ports may conflict with other devcontainers
- ⚠️ **Single Instance**: Cannot run multiple isolated test suites

**Implementation Strategy**:
```yaml
# docker-compose.yml
services:
  azurite:
    image: mcr.microsoft.com/azure-storage/azurite
    ports:
      - "10000:10000"  # Blob
      - "10001:10001"  # Queue  
      - "10002:10002"  # Table
    volumes:
      - azurite_data:/data
    command: ["azurite", "--blobHost", "0.0.0.0", "--queueHost", "0.0.0.0", "--tableHost", "0.0.0.0"]

volumes:
  azurite_data:
```

#### 3. NPM Azurite with Dynamic Port Detection

**Overview**: Install Azurite via NPM and implement port conflict detection.

**Pros**:
- ✅ **Cross-Platform**: Works on any OS with Node.js
- ✅ **Dynamic Ports**: Can detect and avoid port conflicts
- ✅ **Programmatic Control**: Can start/stop from test code

**Cons**:
- ❌ **Node.js Dependency**: Requires Node.js runtime
- ❌ **Complex Setup**: Need to implement port detection logic
- ❌ **Process Management**: Need to handle Azurite process lifecycle

#### 4. In-Memory Storage Alternatives

**Overview**: Use in-memory blob storage implementations for testing.

**Pros**:
- ✅ **No External Dependencies**: Pure .NET solution
- ✅ **Very Fast**: No network or container overhead

**Cons**:
- ❌ **Limited Fidelity**: May not replicate all Azure Storage behaviors
- ❌ **Test Gaps**: Could miss issues that only occur with real Azure Storage

### Recommended Hybrid Approach

Based on the analysis, we recommend implementing **both** approaches for different scenarios:

#### Primary: Testcontainers for Integration Tests
- Use Testcontainers .NET for all Reqnroll/SpecFlow integration tests
- Provides isolation, automatic port management, and zero setup
- Handles the current failing test scenario perfectly

#### Secondary: DevContainer Integration for Development
- Add Azurite to Docker Compose for development convenience
- Useful for debugging, manual testing, and development workflows
- Provides always-available storage for ad-hoc testing

## Implementation Plan

### Phase 1: Testcontainers Integration (Immediate)

1. **Add NuGet Package**:
   ```xml
   <PackageReference Include="Testcontainers.Azurite" Version="3.6.0" />
   ```

2. **Create Test Fixture**:
   ```csharp
   public class AzuriteTestFixture : IAsyncLifetime
   {
       private AzuriteContainer _azuriteContainer;
       
       public string ConnectionString { get; private set; }
       
       public async Task InitializeAsync()
       {
           _azuriteContainer = new AzuriteBuilder().Build();
           await _azuriteContainer.StartAsync();
           ConnectionString = _azuriteContainer.GetConnectionString();
       }
       
       public async Task DisposeAsync()
       {
           await _azuriteContainer.DisposeAsync();
       }
   }
   ```

3. **Update Test Configuration**:
   - Modify `ScenarioDiContainer.cs` to detect Testcontainers connection string
   - Implement fallback configuration hierarchy

### Phase 2: DevContainer Enhancement (Follow-up)

1. **Update Docker Compose**:
   - Add Azurite service to existing `Docker.Compose/docker-compose.yml`
   - Configure proper networking and volumes

2. **Update DevContainer**:
   - Modify `.devcontainer/devcontainer.json` to use docker-compose
   - Add port forwarding configuration

### Phase 3: Smart Configuration System

1. **Configuration Priority**:
   1. Testcontainers connection string (if available)
   2. Environment variables
   3. DevContainer Azurite service
   4. Development storage emulator fallback

2. **Connection String Builder**:
   ```csharp
   public static string GetAzureStorageConnectionString()
   {
       // Check for Testcontainers
       if (TestcontainersConnectionStringProvider.IsAvailable())
           return TestcontainersConnectionStringProvider.GetConnectionString();
           
       // Check for DevContainer service
       if (IsDevContainerEnvironment())
           return "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;...";
           
       // Fallback to development storage
       return "UseDevelopmentStorage=true";
   }
   ```

## Port Management Strategy

### Automatic Port Allocation (Testcontainers)
```
Container Port → Host Port (Automatically Assigned)
10000 → 59273 (random)
10001 → 59274 (random) 
10002 → 59275 (random)
```

### Port Conflict Detection
- Testcontainers handles this automatically
- Docker allocates from ephemeral port range (32768-65535 on Linux)
- Extremely low probability of conflicts

### Port Sharing Prevention
- Each test execution gets isolated ports
- No shared state between test runs
- Parallel test execution supported

## Developer Experience

### Zero Setup Goal Achievement

1. **Clone Repository**: `git clone <repo>`
2. **Run Tests**: `dotnet test` 
3. **Result**: Tests pass automatically
   - Testcontainers downloads Azurite image (first time only)
   - Starts container with random ports
   - Runs tests against isolated storage
   - Cleans up automatically

### Multiple Developer Support

- Each developer's test runs get unique ports
- No coordination required between developers
- Works in codespaces, devcontainers, local development

### CI/CD Compatibility

- GitHub Actions: Docker available, Testcontainers works out-of-box
- Azure DevOps: Docker available in hosted agents
- Self-hosted agents: Just need Docker installed

## Performance Characteristics

### Testcontainers Startup Time
- **First Run**: ~5-10 seconds (image pull + container start)
- **Subsequent Runs**: ~2-3 seconds (container start only)
- **Per Test Class**: Amortized cost across all tests in class

### DevContainer Startup
- **Initial DevContainer Creation**: ~30 seconds (includes Azurite)
- **Daily Startup**: ~5 seconds
- **Runtime**: Always available, no per-test overhead

## Security Considerations

### Network Isolation
- Testcontainers: Each container isolated, no cross-contamination
- DevContainer: Shared service, appropriate for development only

### Credential Management
- All scenarios use development storage account credentials
- No real Azure credentials required
- Safe for CI/CD pipelines

## Monitoring and Troubleshooting

### Connection Issues
```csharp
// Debug connection strings
Console.WriteLine($"Using connection string: {connectionString}");

// Test connectivity
var blobServiceClient = new BlobServiceClient(connectionString);
await blobServiceClient.GetPropertiesAsync(); // Will throw if unable to connect
```

### Port Verification
```bash
# Check what ports Azurite is using
docker ps --format "table {{.Names}}\t{{.Ports}}" | grep azurite

# Test port connectivity
curl http://localhost:59273/devstoreaccount1  # Should return XML
```

### Container Logs
```bash
# View Azurite logs
docker logs <container-id>
```

## Migration Path for Existing Tests

### Current State (Broken)
```csharp
// ScenarioDiContainer.cs:51
.AddJsonFile("local.settings.json", true, true)  // File missing, config null
```

### Immediate Fix (Applied)
```csharp
// Created local.settings.json with:
{
  "RootBlobStorageConfiguration:ConnectionStringPlainText": "UseDevelopmentStorage=true"
}
```

### Future State (Testcontainers)
```csharp
public class ScenarioDiContainer
{
    public ScenarioDiContainer(ScenarioContext scenarioContext)
    {
        // Smart configuration detection
        string azuriteConnectionString = AzuriteConnectionProvider.GetConnectionString();
        
        this.Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>(
                    "RootBlobStorageConfiguration:ConnectionStringPlainText", 
                    azuriteConnectionString)
            })
            .AddEnvironmentVariables()
            .AddJsonFile("local.settings.json", optional: true)
            .Build();
    }
}
```

## Success Metrics

### Immediate Success Criteria
- ✅ All Marain.Tenancy.Storage.Azure.BlobStorage.Specs tests pass
- ✅ No manual setup required for new developers
- ✅ Tests can run in parallel without port conflicts

### Long-term Success Criteria
- ✅ CI/CD pipelines run reliably without flaky test failures
- ✅ Developer onboarding time reduced (no storage emulator setup)
- ✅ Test execution time remains reasonable (<30 seconds for full suite)

## Conclusion

The Hybrid Approach with Testcontainers as the primary solution and DevContainer integration as a development convenience provides the optimal balance of:

- **Zero Setup**: Developers can clone and immediately run tests
- **Port Management**: Automatic allocation prevents conflicts
- **Test Isolation**: Each test run gets clean state
- **Development Experience**: Always-available storage for debugging
- **CI/CD Compatibility**: Works across all build environments

This strategy addresses all the original requirements while providing a robust, scalable foundation for local Azure Storage development and testing.