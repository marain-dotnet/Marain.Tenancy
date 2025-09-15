#!/usr/bin/env pwsh

<#
.SYNOPSIS
Deploy Application Insights monitoring infrastructure for Marain.Tenancy

.DESCRIPTION
This script deploys Application Insights, Log Analytics workspace, alerts, and dashboards
for monitoring the Marain.Tenancy service.

.PARAMETER ResourceGroupName
The name of the resource group where resources will be deployed

.PARAMETER ApplicationInsightsName
The name of the Application Insights resource

.PARAMETER LogAnalyticsWorkspaceName
The name of the Log Analytics workspace

.PARAMETER Environment
The environment name (dev, staging, prod)

.PARAMETER AlertEmailAddresses
Comma-separated list of email addresses for alert notifications

.PARAMETER Location
Azure region for resource deployment (default: eastus)

.PARAMETER RetentionInDays
Data retention period in days (default: 90)

.PARAMETER DailyDataCapGB
Daily data ingestion cap in GB (default: 10)

.PARAMETER SubscriptionId
Azure subscription ID (optional, uses current subscription if not provided)

.EXAMPLE
./deploy-monitoring.ps1 -ResourceGroupName "rg-marain-tenancy-prod" -ApplicationInsightsName "ai-marain-tenancy-prod" -LogAnalyticsWorkspaceName "law-marain-tenancy-prod" -Environment "prod" -AlertEmailAddresses "admin@company.com,devops@company.com"

.EXAMPLE
./deploy-monitoring.ps1 -ResourceGroupName "rg-marain-tenancy-dev" -ApplicationInsightsName "ai-marain-tenancy-dev" -LogAnalyticsWorkspaceName "law-marain-tenancy-dev" -Environment "dev"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $true)]
    [string]$ApplicationInsightsName,
    
    [Parameter(Mandatory = $true)]
    [string]$LogAnalyticsWorkspaceName,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$AlertEmailAddresses = "",
    
    [Parameter(Mandatory = $false)]
    [string]$Location = "eastus",
    
    [Parameter(Mandatory = $false)]
    [int]$RetentionInDays = 90,
    
    [Parameter(Mandatory = $false)]
    [int]$DailyDataCapGB = 10,
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId = ""
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "🚀 Starting Marain.Tenancy monitoring infrastructure deployment" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow

try {
    # Check if Azure CLI is installed
    if (!(Get-Command "az" -ErrorAction SilentlyContinue)) {
        throw "Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/install-azure-cli"
    }

    # Login check
    Write-Host "🔐 Checking Azure CLI authentication..." -ForegroundColor Blue
    $accountInfo = az account show --query "user.name" -o tsv 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️ Not logged in to Azure. Please run 'az login'" -ForegroundColor Yellow
        az login
        if ($LASTEXITCODE -ne 0) {
            throw "Azure login failed"
        }
    }
    Write-Host "✅ Authenticated as: $accountInfo" -ForegroundColor Green

    # Set subscription if provided
    if ($SubscriptionId) {
        Write-Host "🎯 Setting subscription to: $SubscriptionId" -ForegroundColor Blue
        az account set --subscription $SubscriptionId
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to set subscription: $SubscriptionId"
        }
    }

    # Get current subscription
    $currentSubscription = az account show --query "id" -o tsv
    $subscriptionName = az account show --query "name" -o tsv
    Write-Host "📋 Using subscription: $subscriptionName ($currentSubscription)" -ForegroundColor Green

    # Check if resource group exists, create if not
    Write-Host "📦 Checking resource group: $ResourceGroupName" -ForegroundColor Blue
    $rgExists = az group exists --name $ResourceGroupName
    if ($rgExists -eq "false") {
        Write-Host "📦 Creating resource group: $ResourceGroupName" -ForegroundColor Yellow
        az group create --name $ResourceGroupName --location $Location
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create resource group: $ResourceGroupName"
        }
    }
    Write-Host "✅ Resource group ready: $ResourceGroupName" -ForegroundColor Green

    # Prepare deployment parameters
    $deploymentParams = @{
        applicationInsightsName      = $ApplicationInsightsName
        logAnalyticsWorkspaceName    = $LogAnalyticsWorkspaceName
        environment                  = $Environment
        location                     = $Location
        retentionInDays             = $RetentionInDays
        dailyDataCapGB              = $DailyDataCapGB
    }

    # Add email addresses if provided
    if ($AlertEmailAddresses) {
        $emailArray = $AlertEmailAddresses -split ","
        $deploymentParams.alertEmailAddresses = $emailArray
    }

    # Convert parameters to JSON for Bicep deployment
    $paramsJson = $deploymentParams | ConvertTo-Json -Compress

    # Deploy Bicep template
    $deploymentName = "marain-tenancy-monitoring-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Write-Host "🏗️ Deploying monitoring infrastructure..." -ForegroundColor Blue
    Write-Host "Deployment name: $deploymentName" -ForegroundColor Gray

    $bicepFile = Join-Path $ScriptDir "application-insights-setup.bicep"
    if (!(Test-Path $bicepFile)) {
        throw "Bicep template not found at: $bicepFile"
    }

    # Create temporary parameters file
    $tempParamsFile = [System.IO.Path]::GetTempFileName()
    $paramsJson | Out-File -FilePath $tempParamsFile -Encoding UTF8

    try {
        # Deploy using Azure CLI
        $deploymentResult = az deployment group create `
            --resource-group $ResourceGroupName `
            --name $deploymentName `
            --template-file $bicepFile `
            --parameters "@$tempParamsFile" `
            --query "properties.outputs" `
            -o json

        if ($LASTEXITCODE -ne 0) {
            throw "Bicep deployment failed"
        }

        $outputs = $deploymentResult | ConvertFrom-Json
        Write-Host "✅ Infrastructure deployment completed successfully!" -ForegroundColor Green
    }
    finally {
        # Clean up temporary file
        if (Test-Path $tempParamsFile) {
            Remove-Item $tempParamsFile -Force
        }
    }

    # Display deployment results
    Write-Host "`n📊 Deployment Results:" -ForegroundColor Cyan
    Write-Host "Application Insights ID: $($outputs.applicationInsightsId.value)" -ForegroundColor White
    Write-Host "Connection String: $($outputs.applicationInsightsConnectionString.value)" -ForegroundColor White
    Write-Host "Instrumentation Key: $($outputs.applicationInsightsInstrumentationKey.value)" -ForegroundColor White
    Write-Host "Log Analytics Workspace ID: $($outputs.logAnalyticsWorkspaceId.value)" -ForegroundColor White
    
    if ($outputs.actionGroupId.value) {
        Write-Host "Action Group ID: $($outputs.actionGroupId.value)" -ForegroundColor White
    }

    # Deploy dashboard if JSON exists
    $dashboardFile = Join-Path $ScriptDir "application-insights-dashboard.json"
    if (Test-Path $dashboardFile) {
        Write-Host "`n📊 Deploying Application Insights dashboard..." -ForegroundColor Blue
        try {
            # Note: Dashboard deployment requires the dashboard JSON to be updated with actual resource IDs
            Write-Host "⚠️ Dashboard JSON needs to be updated with actual resource IDs before deployment" -ForegroundColor Yellow
            Write-Host "Update the resourceIds in the JSON file with:" -ForegroundColor Yellow
            Write-Host "  - Application Insights ID: $($outputs.applicationInsightsId.value)" -ForegroundColor Gray
            Write-Host "Then run: az portal dashboard import --input-path '$dashboardFile' --resource-group '$ResourceGroupName'" -ForegroundColor Gray
        }
        catch {
            Write-Host "⚠️ Dashboard deployment skipped: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    # Configuration instructions
    Write-Host "`n🔧 Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Update your application configuration with the Application Insights connection string:" -ForegroundColor White
    Write-Host "   ConnectionStrings:ApplicationInsights: '$($outputs.applicationInsightsConnectionString.value)'" -ForegroundColor Gray
    
    Write-Host "2. Update appsettings.json with:" -ForegroundColor White
    Write-Host @"
   {
     "ApplicationInsights": {
       "ConnectionString": "$($outputs.applicationInsightsConnectionString.value)",
       "EnableAdaptiveSampling": true,
       "SamplingSettings": {
         "SamplingPercentage": 100
       }
     }
   }
"@ -ForegroundColor Gray

    Write-Host "3. Verify telemetry is flowing by checking the Application Insights resource in Azure Portal" -ForegroundColor White
    Write-Host "4. Import the dashboard JSON file (after updating resource IDs)" -ForegroundColor White
    Write-Host "5. Test alert rules by triggering error conditions" -ForegroundColor White

    # Environment-specific guidance
    if ($Environment -eq "prod") {
        Write-Host "`n⚠️ Production Environment Notes:" -ForegroundColor Yellow
        Write-Host "- Consider reducing sampling percentage for production (e.g., 10-20%)" -ForegroundColor Yellow
        Write-Host "- Review data retention and daily cap settings based on expected volume" -ForegroundColor Yellow
        Write-Host "- Set up additional monitoring and backup alert channels" -ForegroundColor Yellow
        Write-Host "- Review and test disaster recovery procedures" -ForegroundColor Yellow
    }

    Write-Host "`n✅ Marain.Tenancy monitoring infrastructure deployment completed successfully!" -ForegroundColor Green

}
catch {
    Write-Host "`n❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace:" -ForegroundColor Red
    Write-Host $_.Exception.StackTrace -ForegroundColor Red
    exit 1
}

Write-Host "`n🎉 Deployment completed. Your Marain.Tenancy service is now ready for comprehensive monitoring!" -ForegroundColor Green