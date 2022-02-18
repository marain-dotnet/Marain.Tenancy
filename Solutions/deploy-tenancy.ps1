[CmdletBinding(SupportsShouldProcess)]
param (
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

    [Parameter(Mandatory = $true)]
    [string] $TenancyContainerName,

    [Parameter(Mandatory = $true)]
    [string] $TenancyContainerTag,

    [Parameter()]
    [string] $ConfigPath
)

$ErrorActionPreference = 'Stop'
$InformationPreference = $InformationAction ? $InformationAction : 'Continue'

$here = Split-Path -Parent $PSCommandPath

if (!$ConfigPath) { $ConfigPath = Join-Path $here "config" }

# Install/Import Corvus.Deployment
Install-Module Corvus.Deployment -RequiredVersion 0.3.17 -Scope CurrentUser -Force -Repository PSGallery
Import-Module Corvus.Deployment -Force
Connect-CorvusAzure -SubscriptionId $SubscriptionId -AadTenantId $AadTenantId

function toResourceName($configValue, $serviceName, $resourceShortCode, $uniqueSuffix) {
    return [string]::IsNullOrEmpty($configValue) ? ("{0}{1}{2}" -f $serviceName, $resourceShortCode, $uniqueSuffix): $configValue
}
function Get-UniqueSuffix
{
    [CmdletBinding()]
    param
    (
        [Parameter(Position=0)]
        [string] $SubscriptionId,

        [Parameter(Position=1)]
        [string] $StackName,

        [Parameter(Position=2)]
        [string] $ServiceInstance,

        [Parameter(Position=3)]
        [string] $Environment
    )

    function Get-UniqueString ([string]$id, $length=12)
    {
        $hashArray = (New-Object System.Security.Cryptography.SHA512Managed).ComputeHash($id.ToCharArray())
        -join ($hashArray[1..$length] | ForEach-Object { [char]($_ % 26 + [byte][char]'a') })
    }

    $inputString = "{0}__{1}__{2}__{3}" -f $SubscriptionId, $StackName, $ServiceInstance, $Environment
    return Get-UniqueString $inputString
}

#region Placeholder Configuration
$deploymentConfig = Read-CorvusDeploymentConfig -ConfigPath $ConfigPath  `
                                                -EnvironmentConfigName $Environment `
                                                -Verbose

# $azureAdApplicationsToDeploy = @(
#     @{
#         Name = "{0}.{1}.{2}-{3}.{4}" -f `
#                         $StackName,
#                         $Environment,
#                         $deploymentConfig.ServiceName,
#                         $ServiceInstance,
#                         $deploymentConfig.ApiAppName
#         KeyVaultSecretName = $deploymentConfig.TenancySpCredentialSecretName
#         Roles = @(
#             @{
#                 id = "7619c293-764c-437b-9a8e-698a26250efd"
#                 displayName = "Tenancy administrator"
#                 description = "Ability to create, modify, read, and remove tenants"
#                 value = "TenancyAdministrator"
#                 allowedMemberTypes = @("User","Application")
#             }
#             @{
#                 id = "60743a6a-63b6-42e5-a464-a08698a0e9ed"
#                 displayName = "Tenancy reader"
#                 description = "Ability to read information about tenants"
#                 value = "TenancyReader"
#                 allowedMemberTypes = @("User","Application")
#             }
#         )        
#     }
# )

$serviceName = "tenancy"
$uniqueSuffix = Get-UniqueSuffix -SubscriptionId $SubscriptionId `
                                 -StackName $StackName `
                                 -ServiceInstance $ServiceInstance `
                                 -Environment $Environment

$tenancyResourceGroupName = toResourceName $deploymentConfig.TenancyResourceGroupName $serviceName "rg" $uniqueSuffix
$tenancyStorageName = toResourceName $deploymentConfig.TenancyStorageName $serviceName "st" $uniqueSuffix
$tenancyStorageSku = [string]::IsNullOrEmpty($deploymentConfig.TenancyStorageSku) ? $deploymentConfig.TenancyStorageSku : "Standard_LRS"
$appName = toResourceName $deploymentConfig.TenancyAppName $serviceName "api" $uniqueSuffix
$aadAppName = "{0}.{1}.{2}-{3}.{4}" -f `
                    $StackName,
                    $Environment,
                    $deploymentConfig.ServiceName,
                    $ServiceInstance,
                    $deploymentConfig.ApiAppName
$tenancyAdminSpName = "$serviceName-admin-$uniqueSuffix"

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
if ($deploymentConfig.HostingPlatformType -ieq "functions") {
    if ($deploymentConfig.UseSharedMarainHosting) {
        $hostingPlatformName = toResourceName $deploymentConfig.HostPlatformName "marain" "appsvc" $uniqueSuffix
        $hostingPlatformResourceGroupName = toResourceName $deploymentConfig.HostPlatformResourceGroupName "marain" "rg" $uniqueSuffix
    }
    else {
        $hostingPlatformName = toResourceName $deploymentConfig.HostPlatformName $serviceName "appsvc" $uniqueSuffix
        $hostingPlatformResourceGroupName = toResourceName $deploymentConfig.HostPlatformResourceGroupName $serviceName "rg" $uniqueSuffix
    }
}
else {
    if ($deploymentConfig.UseSharedMarainHosting) {
        $hostingPlatformName = toResourceName $deploymentConfig.HostPlatformName "marain" "kubeenv" $uniqueSuffix
        $hostingPlatformResourceGroupName = toResourceName $deploymentConfig.HostPlatformResourceGroupName "marain" "rg" $uniqueSuffix
    }
    else {
        $hostingPlatformName = toResourceName $deploymentConfig.HostPlatformName $serviceName "kubeenv" $uniqueSuffix
        $hostingPlatformResourceGroupName = toResourceName $deploymentConfig.HostPlatformResourceGroupName $serviceName "rg" $uniqueSuffix
    }
}

# If not specified, derive same default values used by the 'instance' deployment
$acrName = [string]::IsNullOrEmpty($deploymentConfig.AcrName) ? "$($appEnvironmentName)acr" : $deploymentConfig.AcrName
$acrResourceGroupName = [string]::IsNullOrEmpty($deploymentConfig.AcrResourceGroupName) ? $appEnvironmentResourceGroupName : $deploymentConfig.AcrResourceGroupName
$acrSubscriptionId = [string]::IsNullOrEmpty($deploymentConfig.AcrSubscriptionId) ? (Get-AzContext).Subscription.Id : $deploymentConfig.AcrSubscriptionId

# Tags
$defaultTags = @{
    serviceName = $ServiceName
    serviceInstance = $ServiceInstance
    stackName = $StackName
    environment = $Environment
}
#endregion

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
        tenancyAadAppName = $aadAppName
        enableAuth = $false
        tenancySpCredentialSecretName = $deploymentConfig.TenancySpCredentialSecretName
        tenancyAdminSpName = $tenancyAdminSpName
        tenancyAdminSpCredentialSecretName = $deploymentConfig.TenancyAdminSpCredentialSecretName
        tenancyContainerName = $TenancyContainerName
        tenancyContainerTag = $TenancyContainerTag

        aadDeploymentManagedIdentityName = $deploymentConfig.AadDeploymentManagedIdentityName
        aadDeploymentManagedIdentityResourceGroupName = $deploymentConfig.AadDeploymentManagedIdentityResourceGroupName
        aadDeploymentManagedIdentitySubscriptionId = $deploymentConfig.AadDeploymentManagedIdentitySubscriptionId
        
        useExistingKeyVault = $deploymentConfig.UseSharedKeyVault
        existingKeyVaultResourceGroupName = $deploymentConfig.UseSharedKeyVault ? $sharedKeyVaultResourceGroupName : ''
        keyVaultName = $keyVaultName
        
        hostingPlatformType = $deploymentConfig.HostingPlatformType
        useExistingHosting = $deploymentConfig.UseSharedMarainHosting
        hostingPlatformName = $hostingPlatformName
        hostingPlatformResourceGroupName = $hostingPlatformResourceGroupName
        # hostingPlatformSubscriptionId = ""
        
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

Invoke-CorvusArmTemplateDeployment `
    -BicepVersion "0.4.1124" `
    -DeploymentScope $armDeployment.Scope `
    -Location $armDeployment.Location `
    -ArmTemplatePath $armDeployment.TemplatePath `
    -TemplateParameters $armDeployment.TemplateParameters `
    -NoArtifacts `
    -MaxRetries 1 `
    -Verbose `
    -WhatIf:$WhatIfPreference
