#Requires -Version 7
<#
.SYNOPSIS
    Runs the Marain Tenancy Service deployment process.
.DESCRIPTION
    This script was scaffolded using a template from the ZeroFailed project.
    It uses the InvokeBuild module to orchestrate an opinionated Azure deployment process.
.EXAMPLE
    PS C:\> ./deploy.ps1
    Downloads any missing module dependencies (ZeroFailed & InvokeBuild) and executes
    the build process.
.PARAMETER Tasks
    Optionally override the default task executed as the entry-point of the deployment.
.PARAMETER Environment
    The target environment for deployment (e.g., dev, test, uat, prod).
.PARAMETER EnvironmentConfigName
    The name of the environment configuration file. if different from the environment name.
.PARAMETER ConfigPath
    The path to the configuration files.
.PARAMETER ZfModulePath
    The path to import the ZeroFailed module from. This is useful when testing pre-release
    versions of ZeroFailed that are not yet available in the PowerShell Gallery.
.PARAMETER ZfModuleVersion
    The version of the ZeroFailed module to import. This is useful when testing pre-release
    versions of ZeroFailed that are not yet available in the PowerShell Gallery.
.PARAMETER InvokeBuildModuleVersion
    The version of the InvokeBuild module to be used.
#>
[CmdletBinding()]
param (
    [Parameter(Position=0)]
    [string[]] $Tasks = @("."),

    [Parameter(Mandatory = $true)]
    [Alias('Environment')]
    [string] $EnvironmentName,

    [Parameter()]
    [string] $EnvironmentConfigName = $EnvironmentName,

    [Parameter()]
    [Alias('ConfigPath')]
    [string] $EnvironmentConfigPath = 'deployment/config',

    [Parameter()]
    [string] $ImageTag = "dev",

    [Parameter()]
    [string] $ZfModulePath,

    [Parameter()]
    [string] $ZfModuleVersion = "1.0.6",

    [Parameter()]
    [version] $InvokeBuildModuleVersion = "5.12.1"
)
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $PSCommandPath

#region InvokeBuild setup
# This handles calling the build engine when this file is run like a normal PowerShell script
# (i.e. avoids the need to have another script to setup the InvokeBuild environment and issue the 'Invoke-Build' command )
if ($MyInvocation.ScriptName -notlike '*Invoke-Build.ps1') {
    Install-PSResource InvokeBuild -Version $InvokeBuildModuleVersion -Scope CurrentUser -TrustRepository -Verbose:$false | Out-Null
    try {
        Invoke-Build $Tasks $MyInvocation.MyCommand.Path @PSBoundParameters
    }
    catch {
        Write-Host -f Yellow "`n`n***`n*** Build Failure Summary - check previous logs for more details`n***"
        Write-Host -f Yellow $_.Exception.Message
        Write-Host -f Yellow $_.ScriptStackTrace
        throw
    }
    return
}
#endregion

#region Initialise build framework
$splat = @{ Force = $true; Verbose = $false}
Import-Module Microsoft.PowerShell.PSResourceGet
if (!($ZfModulePath)) {
    Install-PSResource ZeroFailed -Version $ZfModuleVersion -Scope CurrentUser -TrustRepository | Out-Null
    $ZfModulePath = "ZeroFailed"
    $splat.Add("RequiredVersion", ($ZfModuleVersion -split '-')[0])
}
else {
    Write-Host "ZfModulePath: $ZfModulePath"
}
$splat.Add("Name", $ZfModulePath)
# Ensure only 1 version of the module is loaded
Get-Module ZeroFailed | Remove-Module
Import-Module @splat
$ver = "{0} {1}" -f (Get-Module ZeroFailed).Version, (Get-Module ZeroFailed).PrivateData.PsData.PreRelease
Write-Host "Using ZeroFailed module version: $ver"
#endregion

$PSModuleAutoloadingPreference = 'none'

# Load the build configuration
. $here/.zf/deploy-config.ps1
