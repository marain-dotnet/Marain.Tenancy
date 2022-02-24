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

    [Parameter()]
    [string] $ConfigPath,

    [Parameter()]
    [switch] $Cleardown
)

$ErrorActionPreference = 'Stop'
$InformationPreference = $InformationAction ? $InformationAction : 'Continue'

$here = Split-Path -Parent $PSCommandPath

if (!$ConfigPath) { $ConfigPath = Join-Path $here "config" }

# Install/Import Corvus.Deployment
$minCorvusVersion = "0.3.18"
Install-Module Corvus.Deployment -MinimumVersion $minCorvusVersion -Scope CurrentUser -Force -Repository PSGallery
Import-Module Corvus.Deployment -MinimumVersion $minCorvusVersion -Force
Connect-CorvusAzure -SubscriptionId $SubscriptionId -AadTenantId $AadTenantId

function toResourceName($configValue, $serviceName, $resourceShortCode, $uniqueSuffix) {
    return [string]::IsNullOrEmpty($configValue) ? ("{0}{1}{2}" -f $serviceName, $resourceShortCode, $uniqueSuffix): $configValue
}

#region Placeholder Configuration
$deploymentConfig = Read-CorvusDeploymentConfig -ConfigPath $ConfigPath  `
                                                -EnvironmentConfigName $Environment `
                                                -Verbose

$ServiceName = $deploymentConfig.ServiceName
$uniqueSuffix = Get-CorvusUniqueSuffix -SubscriptionId $SubscriptionId `
                                       -StackName $StackName `
                                       -ServiceInstance $ServiceInstance `
                                       -Environment $Environment

$tenancyResourceGroupName = toResourceName $deploymentConfig.TenancyResourceGroupName $serviceName "rg" $uniqueSuffix
$tenancyStorageName = toResourceName $deploymentConfig.TenancyStorageName $serviceName "st" $uniqueSuffix
$tenancyStorageSku = [string]::IsNullOrEmpty($deploymentConfig.TenancyStorageSku) ? "Standard_LRS" : $deploymentConfig.TenancyStorageSku
$appName = toResourceName $deploymentConfig.TenancyAppName $serviceName "api" $uniqueSuffix
$tenancyAdminSpName = [string]::IsNullOrEmpty($deploymentConfig.TenancyAdminSpn) ? "$serviceName-admin-$uniqueSuffix" : $deploymentConfig.TenancyAdminSpn

if ($deploymentConfig.UseSharedKeyVault) {
    # If not specified, derive same generated values as the 'instance' deployment
    # TODO: Lookup these details in AppConfig?
    $sharedKeyVaultResourceGroupName = toResourceName $deploymentConfig.SharedKeyVaultResourceGroupName "marain" "rg" $uniqueSuffix
    $keyVaultName = toResourceName $deploymentConfig.SharedKeyVaultName "marain" "kv" $uniqueSuffix
}
else {
    $keyVaultName = toResourceName $deploymentConfig.KeyVaultName $serviceName "kv" $uniqueSuffix
}

# AppConfig settings
# If not specified, derive same generated values as the 'instance' deployment
$appConfigurationStoreName = toResourceName $deploymentConfig.AppConfigurationStoreName "marain" "cfg" $uniqueSuffix
$appConfigurationStoreResourceGroupName = toResourceName $deploymentConfig.AppConfigurationStoreResourceGroupName "marain" "rg" $uniqueSuffix
$appConfigurationStoreSubscriptionId = [string]::IsNullOrEmpty($deploymentConfig.AppConfigurationStoreSubscriptionId) ? (Get-AzContext).Subscription.Id : $deploymentConfig.AppConfigurationStoreSubscriptionId

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
        $hostingPlatformName = toResourceName $deploymentConfig.HostPlatformName "marain" "hosting" $uniqueSuffix
        $hostingPlatformResourceGroupName = toResourceName $deploymentConfig.HostPlatformResourceGroupName "marain" "rg" $uniqueSuffix
    }
    else {
        $hostingPlatformName = toResourceName $deploymentConfig.HostPlatformName $serviceName "hosting" $uniqueSuffix
        $hostingPlatformResourceGroupName = toResourceName $deploymentConfig.HostPlatformResourceGroupName $serviceName "rg" $uniqueSuffix
    }
}


# $instanceResourceGroupName = toResourceName $deploymentConfig.InstanceResourceGroupName $serviceName "rg" $uniqueSuffix
# $keyVaultName = toResourceName $deploymentConfig.KeyVaultName $serviceName "kv" $uniqueSuffix

# $hostingEnvironmentName = toResourceName $deploymentConfig.HostingEnvironmentName $serviceName "kubeenv" $uniqueSuffix
# $hostingEnvironmentResourceGroupName = [string]::IsNullOrEmpty($deploymentConfig.HostingEnvironmentResourceGroupName) ? $instanceResourceGroupName : $deploymentConfig.HostingEnvironmentResourceGroupName
# $hostingEnvironmentSubscriptionId = [string]::IsNullOrEmpty($deploymentConfig.HostingEnvironmentSubscriptionId) ? (Get-AzContext).Subscription.Id : $deploymentConfig.HostingEnvironmentSubscriptionId

# $appConfigStoreName = toResourceName $deploymentConfig.AppConfigurationStoreName $serviceName "cfg" $uniqueSuffix
# $appConfigurationStoreLocation = [string]::IsNullOrEmpty($deploymentConfig.AppConfigurationStoreLocation) ? $deploymentConfig.AzureLocation : $deploymentConfig.AppConfigurationStoreLocation
# $appConfigStoreResourceGroupName = [string]::IsNullOrEmpty($deploymentConfig.AppConfigurationStoreResourceGroupName) ? $instanceResourceGroupName : $deploymentConfig.AppConfigurationStoreResourceGroupName
# $appConfigurationStoreSubscriptionId = [string]::IsNullOrEmpty($deploymentConfig.AppConfigurationStoreSubscriptionId) ? (Get-AzContext).Subscription.Id : $deploymentConfig.AppConfigurationStoreSubscriptionId
# $appConfigurationLabel = "$Environment-$StackName"

# $appInsightsWorkspaceName = [string]::IsNullOrEmpty($deploymentConfig.AppInsightsWorkspaceName) ? "$($hostingEnvironmentName)ai" : $deploymentConfig.AppInsightsWorkspaceName
# $appInsightsWorkspaceLocation = [string]::IsNullOrEmpty($deploymentConfig.AppInsightsWorkspaceLocation) ? $deploymentConfig.AzureLocation : $deploymentConfig.AppInsightsWorkspaceLocation
# $appInsightsWorkspaceResourceGroupName = [string]::IsNullOrEmpty($deploymentConfig.AppInsightsWorkspaceResourceGroupName) ? $instanceResourceGroupName : $deploymentConfig.AppInsightsWorkspaceResourceGroupName
# $appInsightsWorkspaceSubscriptionId = [string]::IsNullOrEmpty($deploymentConfig.AppInsightsWorkspaceSubscriptionId) ? $(Get-AzContext).Subscription.Id : $deploymentConfig.AppInsightsWorkspaceSubscriptionId

# $acrName = toResourceName [string]::IsNullOrEmpty($deploymentConfig.AcrName) ? "$($hostingEnvironmentName)acr" : $deploymentConfig.AcrName
# $acrResourceGroupName = [string]::IsNullOrEmpty($deploymentConfig.AcrResourceGroupName) ? $hostingEnvironmentResourceGroupName : $deploymentConfig.AcrResourceGroupName
# $acrSubscriptionId = [string]::IsNullOrEmpty($deploymentConfig.AcrSubscriptionId) ? (Get-AzContext).Subscription.Id : $deploymentConfig.AcrSubscriptionId


# Tags
$defaultTags = @{
    serviceName = $ServiceName
    serviceInstance = $ServiceInstance
    stackName = $StackName
    environment = $Environment
}
#endregion

# ARM template parameters
# ARM template parameters
$armDeployment = @{
    TemplatePath = Join-Path -Resolve $here "main.bicep"
    Location = $deploymentConfig.AzureLocation
    Scope = "Subscription"
    TemplateParameters = @{
        resourceGroupName = $tenancyResourceGroupName
        storageAccountName = $tenancyStorageName
        storageSku = $tenancyStorageSku
        tenancyStorageSecretName = $deploymentConfig.TenancyStorageSecretName
        tenancyAppName = $appName
        # tenancyAadAppName = $aadAppName
        # enableAuth = $false
        # tenancySpCredentialSecretName = $deploymentConfig.TenancySpCredentialSecretName
        tenancyAdminSpName = $tenancyAdminSpName
        tenancyAdminSpCredentialSecretName = $deploymentConfig.TenancyAdminSpCredentialSecretName
        # tenancyContainerName = $TenancyContainerName
        # tenancyContainerTag = $TenancyContainerTag

        aadDeploymentManagedIdentityName = $deploymentConfig.AadDeploymentManagedIdentityName
        aadDeploymentManagedIdentityResourceGroupName = $deploymentConfig.AadDeploymentManagedIdentityResourceGroupName
        aadDeploymentManagedIdentitySubscriptionId = $deploymentConfig.AadDeploymentManagedIdentitySubscriptionId
        
        useExistingKeyVault = $deploymentConfig.UseSharedKeyVault
        keyVaultName = $keyVaultName
        keyVaultResourceGroupName = $deploymentConfig.UseSharedKeyVault ? $sharedKeyVaultResourceGroupName : ''
        keyVaultSecretsReadersGroupObjectId = $deploymentConfig.KeyVaultReadersGroupObjectId
        keyVaultSecretsContributorsGroupObjectId = $deploymentConfig.KeyVaultContributorsGroupObjectId
        
        hostingEnvironmentType = $deploymentConfig.HostingEnvironmentType
        useExistingHosting = $deploymentConfig.UseSharedMarainHosting
        hostingPlatformName = $hostingPlatformName
        hostingPlatformResourceGroupName = $hostingPlatformResourceGroupName
        # hostingPlatformSubscriptionId = ""
        
        # useAzureContainerRegistry = !$deploymentConfig.UseNonAzureContainerRegistry
        # acrName = $acrName
        # acrResourceGroupName = $acrResourceGroupName
        # acrSubscriptionId = $acrSubscriptionId
        # containerRegistryServer = $deploymentConfig.ContainerRegistryServer
        # containerRegistryUser = $deploymentConfig.ContainerRegistryUser

        appConfigurationStoreName = $appConfigurationStoreName
        appConfigurationStoreResourceGroupName = $appConfigurationStoreResourceGroupName
        appConfigurationStoreSubscription = $appConfigurationStoreSubscriptionId
        appConfigurationLabel = "$Environment-$StackName"

        tenantId = $AadTenantId
        resourceTags = $defaultTags
    }
}

# Remove any parameters that do not have a value, this will allow any defaults
# defined in the ARM template to be used
$parametersWithValues = @{}
$armDeployment.TemplateParameters.Keys |
    ? { !([string]::IsNullOrEmpty($armDeployment.TemplateParameters[$_])) } |
    % { $parametersWithValues += @{ $_ = $armDeployment.TemplateParameters[$_] } }
$parametersWithValues | ft | out-string | Write-Host

if ($Cleardown) {
    Write-Information "Running Cleardown..."
    Remove-AzResourceGroup -Name $instanceResourceGroupName -Force -Verbose
    Remove-AzKeyVault -VaultName $keyVaultName `
                      -Location $deploymentConfig.AzureLocation `
                      -InRemovedState `
                      -Force `
                      -Verbose
}
else {
    Invoke-CorvusArmTemplateDeployment `
        -BicepVersion "0.4.1272" `
        -DeploymentScope $armDeployment.Scope `
        -Location $armDeployment.Location `
        -ArmTemplatePath $armDeployment.TemplatePath `
        -TemplateParameters $parametersWithValues `
        -NoArtifacts `
        -MaxRetries 1 `
        -Verbose `
        -WhatIf:$WhatIfPreference
}
