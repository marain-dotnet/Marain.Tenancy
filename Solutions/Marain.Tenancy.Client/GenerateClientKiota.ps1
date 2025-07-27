#!/usr/bin/env pwsh

# Generate Kiota client for Marain.Tenancy API
# This script replaces the AutoRest-based GenerateClient.ps1

param(
    [string]$ApiUrl = "http://localhost:5000",
    [string]$OutputPath = "Marain/Tenancy/KiotaClient",
    [string]$Namespace = "Marain.Tenancy.KiotaClient"
)

Write-Output "Generating Kiota client from $ApiUrl/swagger/v1/swagger.json"
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
# Note: Using separate namespace initially to enable side-by-side comparison
kiota generate `
    --openapi "$ApiUrl/swagger/v1/swagger.json" `
    --language CSharp `
    --output $FullOutputPath `
    --namespace-name $Namespace `
    --class-name "TenancyApiClient" `
    --exclude-backward-compatible

Write-Output "Kiota client generation completed successfully!"
Write-Output "Generated files in: $FullOutputPath"