# PowerShell script to manage Azurite Docker container
# Usage:
#   .\manage-azurite.ps1 -Action start
#   .\manage-azurite.ps1 -Action stop

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("start", "stop")]
    [string]$Action
)

# Configuration
$ContainerName = "azurite-dev-marain-tenancy"
$ImageName = "mcr.microsoft.com/azure-storage/azurite:latest"
$DataDirectory = "./azurite-data"

# Hardcoded ports to avoid collisions with other Azurite instances
$BlobPort = 10042
$QueuePort = 10043
$TablePort = 10044

function Test-DockerRunning {
    try {
        docker info | Out-Null
        return $true
    }
    catch {
        Write-Error "Docker is not running. Please start Docker Desktop and try again."
        return $false
    }
}

function Start-AzuriteContainer {
    Write-Host "Starting Azurite container..." -ForegroundColor Green
    
    # Check if Docker is running
    if (-not (Test-DockerRunning)) {
        return
    }
    
    # Stop and remove existing container if it exists
    $existingContainer = docker ps -aq -f name=$ContainerName
    if ($existingContainer) {
        Write-Host "Stopping existing Azurite container..." -ForegroundColor Yellow
        docker stop $ContainerName | Out-Null
        docker rm $ContainerName | Out-Null
    }
    
    # Create data directory if it doesn't exist
    if (-not (Test-Path $DataDirectory)) {
        Write-Host "Creating data directory: $DataDirectory" -ForegroundColor Cyan
        New-Item -ItemType Directory -Path $DataDirectory -Force | Out-Null
    }
    
    # Pull latest image
    Write-Host "Pulling Azurite image..." -ForegroundColor Cyan
    docker pull $ImageName | Out-Null
    
    # Start the container
    Write-Host "Starting Azurite container with ports: Blob=$BlobPort, Queue=$QueuePort, Table=$TablePort" -ForegroundColor Cyan
    
    $dockerArgs = @(
        "run", "-d",
        "--name", $ContainerName,
        "-p", "${BlobPort}:10000",
        "-p", "${QueuePort}:10001", 
        "-p", "${TablePort}:10002",
        "-v", "${PWD}/${DataDirectory}:/data",
        $ImageName,
        "azurite", "--blobHost", "0.0.0.0", "--queueHost", "0.0.0.0", "--tableHost", "0.0.0.0", "--location", "/data"
    )
    
    $containerId = docker @dockerArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Azurite container started successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Container Details:" -ForegroundColor Yellow
        Write-Host "  Name: $ContainerName"
        Write-Host "  Image: $ImageName"
        Write-Host "  Data Directory: $DataDirectory"
        Write-Host ""
        Write-Host "Service Endpoints:" -ForegroundColor Yellow
        Write-Host "  Blob Service:  http://localhost:$BlobPort"
        Write-Host "  Queue Service: http://localhost:$QueuePort"
        Write-Host "  Table Service: http://localhost:$TablePort"
        Write-Host ""
        Write-Host "Connection Strings:" -ForegroundColor Yellow
        Write-Host "  Blob:  DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:$BlobPort/devstoreaccount1;"
        Write-Host "  Queue: DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;QueueEndpoint=http://localhost:$QueuePort/devstoreaccount1;"
        Write-Host "  Table: DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://localhost:$TablePort/devstoreaccount1;"
        Write-Host ""
        Write-Host "To stop the container, run: .\manage-azurite.ps1 -Action stop" -ForegroundColor Cyan
    }
    else {
        Write-Error "Failed to start Azurite container"
    }
}

function Stop-AzuriteContainer {
    Write-Host "Stopping Azurite container..." -ForegroundColor Yellow
    
    # Check if Docker is running
    if (-not (Test-DockerRunning)) {
        return
    }
    
    # Check if container exists and is running
    $runningContainer = docker ps -q -f name=$ContainerName
    $existingContainer = docker ps -aq -f name=$ContainerName
    
    if ($runningContainer) {
        Write-Host "Stopping running container..." -ForegroundColor Yellow
        docker stop $ContainerName | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Azurite container stopped successfully!" -ForegroundColor Green
        }
        else {
            Write-Error "Failed to stop Azurite container"
            return
        }
    }
    elseif ($existingContainer) {
        Write-Host "Container exists but is not running" -ForegroundColor Yellow
    }
    else {
        Write-Host "No Azurite container found" -ForegroundColor Yellow
        return
    }
    
    # Remove the container
    if ($existingContainer) {
        Write-Host "Removing container..." -ForegroundColor Yellow
        docker rm $ContainerName | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Container removed successfully!" -ForegroundColor Green
        }
        else {
            Write-Error "Failed to remove container"
        }
    }
}

# Main execution
switch ($Action.ToLower()) {
    "start" {
        Start-AzuriteContainer
    }
    "stop" {
        Stop-AzuriteContainer
    }
}