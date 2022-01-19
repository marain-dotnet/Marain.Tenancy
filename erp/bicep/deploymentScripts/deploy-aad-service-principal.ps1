[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string] $DisplayName,

    [Parameter(Mandatory = $true)]
    [string] $KeyVaultName,

    [Parameter(Mandatory = $true)]
    [string] $KeyVaultSecretName,

    [switch] $RotateSecret,

    $CorvusModulePath = $null
)
 
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 4.0

# Workaround current deployScripts limitation only available with v6.6
# We want to use 7.x to take advantage of MS Graph (rather than the deprecated Azure Graph)
# Update-Module Az.Resources -Verbose -Force

$azCtx = Get-AzContext
$azCtx | Format-List | Out-String | Write-Host

if ($CorvusModulePath) {
    # Supports local testing with development versions of Corvus.Deployment
    Write-Verbose "Loading Corvus.Deployment from $CorvusModulePath"
    Import-Module $CorvusModulePath -Force -Verbose:$false
}
else {
    # Install the Corvus.Deployment module from PSGallery
    Install-Module Corvus.Deployment -Scope CurrentUser -Repository PSGallery -Force -Verbose
    Import-Module Corvus.Deployment -Verbose:$false
}
Get-Module Corvus.Deployment | Format-Table | Out-String | Write-Host
Connect-CorvusAzure -SubscriptionId $azCtx.Subscription.Id -AadTenantId $azCtx.Tenant.Id -SkipAzureCli

$sp,$secret = Assert-CorvusAzureServicePrincipalForRbac -Name $DisplayName `
                                                        -KeyVaultName $KeyVaultName `
                                                        -KeyVaultSecretName $KeyVaultSecretName `
                                                        -RotateSecret:$RotateSecret `
                                                        -Verbose:$VerbosePreference

Write-Verbose "Service Principal Details:`n$($sp | ConvertTo-Json)"


# Setup ARM outputs
$DeploymentScriptOutputs = @{
    appId = $sp.ApplicationId       # assuming Azure Graph version of Azure PowerShell
    objectId = $sp.Id
}
Write-Verbose "Script Outputs:`n$($DeploymentScriptOutputs | ConvertTo-Json)"