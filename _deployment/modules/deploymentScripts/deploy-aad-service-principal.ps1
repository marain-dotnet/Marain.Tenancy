[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string] $DisplayName,

    [Parameter(Mandatory = $true)]
    [string] $KeyVaultName,

    [Parameter(Mandatory = $true)]
    [string] $KeyVaultSecretName,

    [int] $PasswordLifetimeDays = 90,

    [string] $CredentialDisplayName = "marain-automation",

    [switch] $UseApplicationCredential,

    [switch] $RotateSecret,

    $CorvusModulePath = $null
)
 
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 4.0

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
Connect-CorvusAzure -AadTenantId $azCtx.Tenant.Id -SkipAzureCli -TenantOnly

$sp,$secret = Assert-CorvusAzureServicePrincipalForRbac -Name $DisplayName `
                                                        -UseApplicationCredential:$UseApplicationCredential `
                                                        -CredentialDisplayName $CredentialDisplayName `
                                                        -PasswordLifetimeDays $PasswordLifetimeDays `
                                                        -KeyVaultName $KeyVaultName `
                                                        -KeyVaultSecretName $KeyVaultSecretName `
                                                        -RotateSecret:$RotateSecret `
                                                        -Verbose:$VerbosePreference

Write-Verbose "Service Principal Details:`n$($sp | ConvertTo-Json)"


# Setup ARM outputs
$DeploymentScriptOutputs = @{
    appId = $sp.appId       # assuming Azure Graph version of Azure PowerShell
    objectId = $sp.id
}
Write-Verbose "Script Outputs:`n$($DeploymentScriptOutputs | ConvertTo-Json)"