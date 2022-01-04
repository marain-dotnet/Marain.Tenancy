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
Import-Module $here/../../Corvus.Deployment/module/Corvus.Deployment.psd1 -Force
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

$azureAdApplicationsToDeploy = @(
    @{
        Name = "{0}.{1}.{2}-{3}.{4}" -f `
                        $StackName,
                        $Environment,
                        $deploymentConfig.ServiceName,
                        $ServiceInstance,
                        $deploymentConfig.ApiAppName
        KeyVaultSecretName = $deploymentConfig.TenancySpCredentialSecretName
        Roles = @(
            @{
                id = "7619c293-764c-437b-9a8e-698a26250efd"
                displayName = "Tenancy administrator"
                description = "Ability to create, modify, read, and remove tenants"
                value = "TenancyAdministrator"
                allowedMemberTypes = @("User","Application")
            }
            @{
                id = "60743a6a-63b6-42e5-a464-a08698a0e9ed"
                displayName = "Tenancy reader"
                description = "Ability to read information about tenants"
                value = "TenancyReader"
                allowedMemberTypes = @("User","Application")
            }
        )        
    }
)

$serviceName = "tenancy"
$uniqueSuffix = Get-UniqueSuffix -SubscriptionId $SubscriptionId `
                                 -StackName $StackName `
                                 -ServiceInstance $ServiceInstance `
                                 -Environment $Environment

$tenancyResourceGroupName = toResourceName $deploymentConfig.TenancyResourceGroupName $serviceName "rg" $uniqueSuffix
$tenancyStorageName = toResourceName $deploymentConfig.TenancyStorageName $serviceName "st" $uniqueSuffix
$tenancyStorageSku = [string]::IsNullOrEmpty($deploymentConfig.TenancyStorageSku) ? $deploymentConfig.TenancyStorageSku : "Standard_LRS"
$appName = toResourceName $deploymentConfig.TenancyAppName $serviceName "api" $uniqueSuffix

# AppConfig settings
# If not specified, derive same generated values as the 'instance' deployment
$appConfigurationStoreName = toResourceName $deploymentConfig.AppConfigurationStoreName "marain" "cfg" $uniqueSuffix
$appConfigurationStoreResourceGroupName = toResourceName $deploymentConfig.AppConfigurationStoreResourceGroupName "marain" "rg" $uniqueSuffix

if ($deploymentConfig.UseSharedKeyVault) {
    # If not specified, derive same generated values as the 'instance' deployment
    # TODO: Lookup these details in AppConfig?
    $sharedKeyVaultResourceGroupName = toResourceName $deploymentConfig.SharedKeyVaultResourceGroupName "marain" "rg" $uniqueSuffix
    $keyVaultName = toResourceName $deploymentConfig.SharedKeyVaultName "marain" "kv" $uniqueSuffix
}
else {
    $keyVaultName = toResourceName $deploymentConfig.KeyVaultName $serviceName "kv" $uniqueSuffix
}

# If not specified, derive same generated values as the 'instance' deployment
# TODO: Lookup details from AppConfig?
$appEnvironmentResourceGroupName = toResourceName $deploymentConfig.AppEnvironmentResourceGroupName "marain" "rg" $uniqueSuffix
$appEnvironmentName = toResourceName $deploymentConfig.AppEnvironmentName "marain" "kubeenv" $uniqueSuffix

# If not specified, derive same default values used by the 'instance' deployment
$acrName = [string]::IsNullOrEmpty($deploymentConfig.AcrName) ? "$($appEnvironmentName)acr" : $deploymentConfig.AcrName
$acrResourceGroupName = [string]::IsNullOrEmpty($deploymentConfig.AcrResourceGroupName) ? $appEnvironmentResourceGroupName : $deploymentConfig.AcrResourceGroupName
$acrSubscriptionId = [string]::IsNullOrEmpty($deploymentConfig.AcrSubscriptionId) ? (Get-AzContext).Subscription.Id : $deploymentConfig.AcrSubscriptionId

# TODO: Add support for the tenant admin SPN and using the marain-cli
# $tenantAdminAppId = try { $deploymentConfig.TenantAdminAppId } catch { $null }
# $tenantAdminSecretName = "DefaultTenantAdminPassword"
# $defaultTenantAdminSpName = 'marain.{0}.{1}.tenantadmin' -f $StackName, $Environment

# Tags
$defaultTags = @{
    serviceName = $ServiceName
    serviceInstance = $ServiceInstance
    stackName = $StackName
    environment = $Environment
}

# ARM template parameters
$armDeployment = @{
    TemplatePath = Join-Path -Resolve $here "Marain.Tenancy.Deployment" "tenancy-main.bicep"
    Location = $deploymentConfig.AzureLocation
    Scope = "Subscription"
    TemplateParameters = @{
        resourceGroupName = $tenancyResourceGroupName
        storageAccountName = $tenancyStorageName
        storageSku = $tenancyStorageSku
        tenancyStorageSecretName = $deploymentConfig.TenancyStorageSecretName
        tenancyAppName = $appName
        tenancySpCredentialSecretName = $deploymentConfig.TenancySpCredentialSecretName
        
        useExistingKeyVault = $deploymentConfig.UseSharedKeyVault
        existingKeyVaultResourceGroupName = $deploymentConfig.UseSharedKeyVault ? $sharedKeyVaultResourceGroupName : ''
        keyVaultName = $keyVaultName
        
        appEnvironmentName = $appEnvironmentName
        appEnvironmentResourceGroupName = $appEnvironmentResourceGroupName
        # appEnvironmentSubscription = ""
        
        useAzureContainerRegistry = !$deploymentConfig.UseNonAzureContainerRegistry
        acrName = $acrName
        acrResourceGroupName = $acrResourceGroupName
        acrSubscriptionId = $acrSubscriptionId
        containerRegistryServer = $deploymentConfig.ContainerRegistryServer
        containerRegistryUser = $deploymentConfig.ContainerRegistryUser

        appConfigurationStoreName = $appConfigurationStoreName
        appConfigurationStoreResourceGroupName = $appConfigurationStoreResourceGroupName
        # appConfigurationStoreSubscription = ""
        appConfigurationLabel = "$Environment-$StackName"

        tenantId = $AadTenantId
        resourceTags = $defaultTags
    }
}
#endregion

# TODO: Add support for the tenant admin SPN and using the marain-cli
#region Custom tasks
# task installMarainCli -After EnsurePreReqs {
#     exec { Install-DotNetTool -Name marain -Version 2.0.0 -Global $true }
# }
# task createTenantAdminServicePrincipal -After PostAzureAd {
#     # Ensure a default tenancy administrator principal is setup/available
#     if (!$tenantAdminAppId -and !$DoNotUseGraph) {
#         # look-up from AAD directly, if we have access
#         $existingSp = Get-AzADServicePrincipal -DisplayName $defaultTenantAdminSpName        
#         if ($existingSp) {
#             $tenantAdminAppId = $existingSp.ApplicationId
#         }
#         else {
#             Write-Host "Creating default tenancy administrator service principal"
#             $newSp = New-AzADServicePrincipal -DisplayName $defaultTenantAdminSpName -SkipAssignment

#             Set-AzKeyVaultSecret -VaultName $sharedKeyVaultName -Name $tenantAdminSecretName -SecretValue $newSp.Secret | Out-Null
#         }
#     }
#     else {
#         Write-Error "Unable to determine the AAD ApplicationId for default tenancy admnistrator - No graph token available and TenantAdminId not supplied"
#     }

#     # Read the tenant admin credentials
#     $tenantAdminSecret = Get-AzKeyVaultSecret -VaultName $sharedKeyVaultName -Name $tenantAdminSecretName

#     # Add guard rails for if we've lost the keyvault secret, re-generate a new one (if we have AAD access)
#     if (!$tenantAdminSecret -and !$DoNotUseGraph) {
#         Write-Host "Resetting credential for default tenancy admnistrator service principal"
#         $newSpCred = $existingSp | New-AzADServicePrincipalCredential
#         Set-AzKeyVaultSecret -VaultName $sharedKeyVaultName -Name $tenantAdminSecretName -SecretValue $newSpCred.Secret | Out-Null
#         $tenantAdminSecret = Get-AzKeyVaultSecret -VaultName $sharedKeyVaultName -Name $tenantAdminSecretName
#     }
#     elseif (!$tenantAdminSecret) {
#         Write-Error "The default tenancy administrator credential was not available in the keyvault - access to AAD is required to resolve this issue"
#     }
# }
# task initialiseTenancyService {
#     # Setup environment variables for marain cli
#     $env:TenancyClient:TenancyServiceBaseUri = "https://{0}.azurewebsites.net/" -f $functionsAppName
#     $env:TenancyClient:ResourceIdForMsiAuthentication = $appRegistration.ApplicationId
#     $env:AzureServicesAuthConnectionString = "RunAs=App;AppId={0};TenantId={1};AppKey={2}" -f `
#                                                         $tenantAdminAppId,
#                                                         $AadTenantId,
#                                                         (ConvertFrom-SecureString $tenantAdminSecret.SecretValue -AsPlainText)

#     # Ensure the tenancy instance is initialised
#     Write-Host "Initialising marain tenancy instance..."
#     $scriptBlock = {  
#         $cliOutput = & marain init
#         if ($LASTEXITCODE -ne 0) {
#             Write-Error "Error whilst trying to initialise the marain tenancy instance: ExitCode=$LASTEXITCODE`n$cliOutput"
#         }
#     }
    
#     Invoke-CorvusCommandWithRetry `
#         -Command $scriptBlock `
#         -RetryCount 5 `
#         -RetryDelay 30
# }
#endregion

# Synopsis: Provision and Deploy
task . FullDeploy

task PostProvision {
    $ArmDeploymentOutputs | fl | Out-String | Write-Host

    # Set the reply-url for the AAD app now we have the FQDN for the service
    $appName = $azureAdApplicationsToDeploy[0].Name
    $appId = Invoke-CorvusAzCli "ad app list --all --query `"[?displayName == '$appName'].appId`" -o tsv" -AsJson
    $replyUrl = "https://{0}/.auth/login/aad/callback" -f $ArmDeploymentOutputs.Outputs.service_url.Value
    Invoke-CorvusAzCli "ad app update --id `"$appId`" --reply-urls $replyUrl"
}