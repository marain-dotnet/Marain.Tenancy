#!/usr/bin/env pwsh

# Generate Kiota client for Marain.Tenancy API

param(
    [string]$ApiUrl = "http://localhost:5138",
    [string]$OutputPath = "Marain/Tenancy/Client",
    [string]$Namespace = "Marain.Tenancy.Client"
)

Write-Output "Generating Kiota client from $ApiUrl/swagger"
Write-Output "Output path: $OutputPath"
Write-Output "Namespace: $Namespace"

# Ensure output directory exists
$FullOutputPath = Join-Path $PSScriptRoot $OutputPath
if (Test-Path $FullOutputPath) {
    Write-Output "Cleaning existing output directory..."
    Remove-Item $FullOutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $FullOutputPath -Force | Out-Null

# Generate Kiota client
kiota generate `
    --openapi "$ApiUrl/swagger" `
    --language CSharp `
    --output $FullOutputPath `
    --namespace-name $Namespace `
    --class-name "TenancyApiClient" `
    --exclude-backward-compatible `
    --serializer none

Write-Output "Kiota client generation completed successfully!"
Write-Output "Generated files in: $FullOutputPath"