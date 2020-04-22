[CmdletBinding()]
param
(
    [switch] $Provision,
    [switch] $Deploy,
    [string] $ReleaseVersion = $null,
    [string] $ConfigPath = $null
)

$ErrorActionPreference = 'Stop'
$VerbosePreference = 'Continue'
Set-StrictMode -Version 3.0

$here = Split-Path -Parent $PSCommandPath

# Import-Module endjin.deployment
Import-Module $here/../../Marain.Instance/_deployContainer/endjin.deployment -Force

if (-not $ConfigPath)
{
    $ConfigPath = Join-Path $here '../../Marain.Configuration'
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
Write-Verbose "here: $here"
. $here/config.ps1

# debug logging of the config
$deployConfig | Format-Table | Out-String | Write-Verbose

# TODO: Currently combined as ServiceContext has dependency on underlying DeploymentContext class
$DeploymentContext,$ServiceDeploymentContext = New-AzureDeploymentContext -AzureLocation $deployConfig.AzureLocation `
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

if ($ReleaseVersion)
{
    # Get github release
}

# $ServiceDeploymentContext = New-AzureServiceDeploymentContext -DeploymentContext $DeploymentContext `
#                                                               -ServiceApiSuffix $deployConfig.MarainServices['Marain.Tenancy'].apiPrefix `
#                                                               -GitHubRelease 'foo' `
#                                                               -ServiceShortName $deployConfig.MarainServices['Marain.Tenancy'].apiPrefix `
#                                                               -TempFolder 'foo'


# ensure AD app
$AppServiceIdentityDetails  = Ensure-AzureAdAppForAppService `
                                        -ServiceDeploymentContext $ServiceDeploymentContext
                                        # -AppNameSuffix $deployConfig.MarainServices['Marain.Tenancy'].apiPrefix


# ensure AD app permissions
foreach ($role in $RequiredAppRoles)
{
    $roleParams = $role + @{ DeploymentContext = $DeploymentContext
                                AppId = $AppServiceIdentityDetails.Application.ApplicationId
                        }
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

    $TenancyAuthAppId = $ServiceDeploymentContext.AdApps[$ServiceDeploymentContext.AppName].ApplicationId.ToString()
    $TemplateParameters = @{
        appName = "tenancy"
        functionEasyAuthAadClientId = $TenancyAuthAppId
        appInsightsInstrumentationKey = $deployConfig.AppInsightsInstrumentationKey
        marainPrefix = $DeploymentContext.Prefix
        environmentSuffix = $DeploymentContext.EnvironmentSuffix
    }
    $DeploymentResult = DeployArmTemplate -TemplateFileName $TemplatePath `
                                          -TemplateParameters $TemplateParameters `
                                          -DeploymentContext $DeploymentContext `
                                          -ArtifactsFolderPath $LinkedTemplatesPath
    
    $ServiceDeploymentContext.AppServices[$ServiceDeploymentContext.AppName] = @{
        AuthAppId = $TenancyAuthAppId
        ServicePrincipalId = $DeploymentResult.Outputs.functionServicePrincipalId.Value
        BaseUrl = $ServiceDeploymentContext.AdApps[$ServiceDeploymentContext.AppName].BaseUrl
    }
}


# app deployment
# if ($Deploy)
if ($True)
{
    $ServiceDeploymentContext.MakeAppServiceCommonService("Marain.Tenancy")

    $ServiceDeploymentContext.UploadReleaseAssetAsAppServiceSitePackage(
        "Marain.Tenancy.Host.Functions.zip",
        $ServiceDeploymentContext.AppName
    )
}
