[CmdletBinding()]
param (
    [Parameter(Position=0)]
    [string[]] $Tasks = @("."),

    [Parameter(Mandatory = $true)]
    [string] $SubscriptionId,
    
    [Parameter(Mandatory = $true)]
    [string] $AadTenantId,

    [Parameter(Mandatory = $true)]
    [string] $StackName,

    [Parameter(Mandatory = $true)]
    [string] $ServiceInstance,

    [Parameter(Mandatory = $true)]
    [string] $Environment,

    [Parameter()]
    [string] $ConfigPath,

    [Parameter()]
    [string] $DeployModulePath
)

$ErrorActionPreference = 'Stop'
$InformationPreference = $InformationAction ? $InformationAction : 'Continue'

$here = Split-Path -Parent $PSCommandPath

if (!$ConfigPath) { $ConfigPath = Join-Path $here "config" }

#region InvokeBuild setup
if (!(Get-Module -ListAvailable InvokeBuild)) {
    Install-Module InvokeBuild -RequiredVersion 5.7.1 -Scope CurrentUser -Force -Repository PSGallery
}
Import-Module InvokeBuild
# This handles calling the build engine when this file is run like a normal PowerShell script
# (i.e. avoids the need to have another script to setup the InvokeBuild environment and issue the 'Invoke-Build' command )
if ($MyInvocation.ScriptName -notlike '*Invoke-Build.ps1') {
    try {
        Invoke-Build $Tasks $MyInvocation.MyCommand.Path @PSBoundParameters
    }
    catch {
        $_.ScriptStackTrace
        throw
    }
    return
}
#endregion

#region Import shared tasks and initialise deploy framework
if (!($DeployModulePath)) {
    if (!(Get-Module -ListAvailable Endjin.RecommendedPractices.Deploy)) {
        Write-Information "Installing 'Endjin.RecommendedPractices.Deploy' module..."
        Install-Module Endjin.RecommendedPractices.Deploy -RequiredVersion 0.0.1 -AllowPrerelease -Scope CurrentUser -Force -Repository PSGallery
    }
    $DeployModulePath = "Endjin.RecommendedPractices.Deploy"
}
else {
    Write-Information "DeployModulePath: $DeployModulePath"
}
Import-Module $DeployModulePath -Force
#endregion

# TODO: Import Corvus.Deployment
Import-Module $here/../../Corvus.Deployment/module/Corvus.Deployment.psd1
Connect-CorvusAzure -SubscriptionId $SubscriptionId -AadTenantId $AadTenantId

# Load the deploy process & tasks
. Endjin.RecommendedPractices.Deploy.tasks

#region Custom functions
function toResourceName($configValue, $serviceName, $resourceShortCode, $uniqueSuffix) {
    return [string]::IsNullOrEmpty($configValue) ? ("{0}{1}{2}" -f $serviceName, $resourceShortCode, $uniqueSuffix): $configValue
}
#endregion

#region Placeholder Configuration
$deploymentConfig = Read-CorvusDeploymentConfig -ConfigPath $ConfigPath  `
                                                -EnvironmentConfigName $Environment `
                                                -Verbose

$ServiceName = "marain"
$uniqueSuffix = Get-UniqueSuffix -SubscriptionId $SubscriptionId `
                                 -StackName $StackName `
                                 -ServiceInstance $ServiceInstance `
                                 -Environment $Environment

$instanceResourceGroupName = toResourceName $deploymentConfig.instanceResourceGroupName $serviceName "rg" $uniqueSuffix
$keyVaultName = toResourceName $deploymentConfig.KeyVaultName $serviceName "kv" $uniqueSuffix
$appEnvironmentName = toResourceName $deploymentConfig.AppEnvironmentName "marain" "kubeenv" $uniqueSuffix
$appConfigStoreName = toResourceName $deploymentConfig.AppConfigurationStoreName "cfg" $uniqueSuffix
$appConfigStoreResourceGroupName = [string]::IsNullOrEmpty($deploymentConfig.AppConfigurationStoreResourceGroupName) ? $instanceResourceGroupName : $deploymentConfig.AppConfigurationStoreResourceGroupName
$appConfigurationLabel = "$Environment-$StackName"

# Tags
$defaultTags = @{
    serviceName = $ServiceName
    serviceInstance = $ServiceInstance
    stackName = $StackName
    environment = $Environment
}

# ARM template parameters
$armDeployment = @{
    TemplatePath = Join-Path -Resolve $here "Marain.Tenancy.Deployment" "instance-main.bicep"
    Location = $deploymentConfig.AzureLocation
    Scope = "Subscription"
    TemplateParameters = @{
        resourceGroupName = $instanceResourceGroupName
        keyVaultName = $keyVaultName
        useExistingAppConfigurationStore = $deploymentConfig.UseExistingAppConfigurationStore
        appConfigurationStoreName = $appConfigStoreName
        appConfigurationStoreResourceGroupName = $appConfigStoreResourceGroupName
        appConfigurationLabel = $appConfigurationLabel
        useExistingAppEnvironment = $false
        appEnvironmentName = $appEnvironmentName
        appEnvironmentResourceGroupName = $instanceResourceGroupName
        includeAcr = !$deploymentConfig.UseExistingAcr
        tenantId = $AadTenantId
        resourceTags = $defaultTags
    }
}
#endregion

# Synopsis: Provision and Deploy
task . FullDeploy

task PostProvision {
    $ArmDeploymentOutputs | fl | Out-String | Write-Host
}
