[CmdletBinding()]
param
(
    [switch] $Provision,
    [switch] $Deploy,
    [string] $ReleaseVersion = $null,
    [string] $ConfigPath = $null
)

#
# WhatIf support through the stack? (inc. ARM)
#

$ErrorActionPreference = 'Stop'
$VerbosePreference = 'Continue'
Set-StrictMode -Version 3.0

# explicitly import this module as it seems to leak a '$here' variable that clashes with ours :-(
Import-Module powershell-yaml

$here = Split-Path -Parent $PSCommandPath

# Import-Module endjin.deployment
Import-Module $here/../../Marain.Instance/_deployContainer/endjin.deployment -Force

if (-not $ConfigPath)
{
    $ConfigPath = Join-Path $here '../../Marain.Instance/_configRepo'
}

# load config from 'shared repo'
# NOTE: In future these would be woven-in directly from git
#       (rather than part of the VSCode workspace as they are now)
$commonConfigPath = Join-Path $ConfigPath 'common/config.yml'
$commonConfig = Get-Content -raw $commonConfigPath | ConvertFrom-Yaml
$environmentConfigPath = Join-Path $ConfigPath 'local/config.yml'
$environmentConfig = Get-Content -raw $environmentConfigPath | ConvertFrom-Yaml

# merge the 'local' app-specific config with the settings from the shared config
# NOTE: this should probably become YAML too, but being able to use powershell expressions is convenient?!?
. $here/config.ps1

# debug logging of the config
$deployConfig | Format-Table | Out-String | Write-Verbose

#
# This is mostly naming-convention configuration and tracking config from provisioned objects
# If we adopt configuration persistence, then it might if not completely disappear at least
# lose its statefulness behaviour
New-AzureDeploymentContext -AzureLocation $deployConfig.AzureLocation `
                            -EnvironmentSuffix $deployConfig.EnvironmentSuffix `
                            -Prefix "mar" `
                            -Name "tenancy" `
                            -AadTenantId $deployConfig.AadTenantId `
                            -SubscriptionId $deployConfig.SubscriptionId `
                            -AadAppIds $deployConfig.AadAppIds `
                            -DoNotUseGraph $deployConfig.DoNotUseGraph `
                            -IncludeServiceContext `
                            -ServiceApiSuffix $deployConfig.MarainServices['Marain.Tenancy'].apiPrefix `
                            -ServiceShortName $deployConfig.MarainServices['Marain.Tenancy'].apiPrefix

#
# Alternatives to tracking state:
# - use convention-based naming to query Azure directly for required info
# - storing info in tags to help future lookup?
#

if ($ReleaseVersion)
{
    # TODO: Get github release for non 'workcopy copy' deployments
}

New-AzureServiceDeploymentContext -ServiceApiSuffix $deployConfig.MarainServices['Marain.Tenancy'].apiPrefix `
                                    -GitHubRelease 'foo' `
                                    -ServiceShortName $deployConfig.MarainServices['Marain.Tenancy'].apiPrefix `
                                    -TempFolder 'foo'


# ensure AD app
$AppServiceIdentityDetails  = Ensure-AzureAdAppForAppService


# ensure AD app permissions
foreach ($role in $RequiredAppRoles)
{
    $roleParams = $role + @{ AppId = $AppServiceIdentityDetails.Application.ApplicationId }
    if ( !(Ensure-AppRolesContain @roleParams) ) {
        Write-Warning "Unable to check AzureAD App roles - no graph access?"
    }
}


# cloud provisioning
# if ($Provision)
if ($True)
{
    $TemplatePath = Join-Path $here '../Solutions/Marain.Tenancy.Deployment/deploy.json' -Resolve
    $LinkedTemplatesPath = Join-Path $here '../Solutions/Marain.Tenancy.Deployment' -Resolve        # folder structure needs fixing

    $TenancyAuthAppId = $script:ServiceDeploymentContext.AdApps[$script:ServiceDeploymentContext.AppName].ApplicationId.ToString()
    $TemplateParameters = @{
        appName = "tenancy"
        functionEasyAuthAadClientId = $TenancyAuthAppId
        appInsightsInstrumentationKey = $deployConfig.AppInsightsInstrumentationKey
        marainPrefix = $script:DeploymentContext.Prefix
        environmentSuffix = $script:DeploymentContext.EnvironmentSuffix
    }
    $DeploymentResult = DeployArmTemplate -TemplateFileName $TemplatePath `
                                          -TemplateParameters $TemplateParameters `
                                          -ArtifactsFolderPath $LinkedTemplatesPath
    
    Set-AppServiceDetails -ServicePrincipalId $DeploymentResult.Outputs.functionServicePrincipalId.Value
}


# app deployment
# if ($Deploy)
if ($True)
{
    Set-AppServiceCommonService -AppKey "Marain.Tenancy"

    UploadReleaseAssetAsAppServiceSitePackage -AssetName "Marain.Tenancy.Host.Functions.zip" `
                                              -AppServiceName $script:ServiceDeploymentContext.AppName
}
