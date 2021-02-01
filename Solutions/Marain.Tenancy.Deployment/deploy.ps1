param (
    [Parameter(Mandatory = $true)]
    [string] $SubscriptionId,
    
    [Parameter(Mandatory = $true)]
    [string] $AadTenantId,

    [Parameter(Mandatory = $true)]
    [string]
    $StackName,

    [Parameter(Mandatory = $true)]
    [string]
    $ServiceInstance,

    [string] $ConfigPath = "./config",

    [Parameter(Mandatory = $true)]
    [string] $Environment,

    [Parameter(Mandatory=$true)]
    [string] $ApplicationZipPath,

    [switch] $ApplicationDeploymentOnly,

    [switch] $DoNotUseGraph
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 4

$here = Split-Path -Parent $PSCommandPath

# Setup the Corvus.Deployment module
$deployModuleVersion = "0.2.6-beta0001"           # control which version of the Corvus.Deployment module to use
Write-Host "Installing Corvus.Deployment module..."
Install-Module -Name Corvus.Deployment -RequiredVersion $deployModuleVersion -Force -AllowPrerelease

Write-Host "Importing Corvus.Deployment module..."
Import-Module Corvus.Deployment -RequiredVersion $deployModuleVersion.Split('-')[0] -ArgumentList @($SubscriptionId, $AadTenantId) -Force

# Setup the Marain CLI
$marainCliVersion = "2.0.0"
Write-Host "Installing Marain CLI..."
dotnet tool install --global marain --version $marainCliVersion

try {
    if ( !(Test-Path $ApplicationZipPath) ) {
        Write-Error "The application deployment ZIP package '$ApplicationZipPath' could not be found"
    }

    # load the common and environment-specific configuration
    $deploymentConfig = Read-CorvusDeploymentConfig -ConfigPath $ConfigPath  `
                                                    -EnvironmentConfigName $Environment `
                                                    -Verbose

    # Ensure we are logged-in to the azure-cli
    Assert-CorvusAzCliLogin -SubscriptionId $SubscriptionId -AadTenantId $AadTenantId

    # Setup the AzureAD app registration for the API app, which requires authentication
    $appRegistrationName = "{0}.{1}.{2}-{3}.{4}" -f $StackName, $Environment, $deploymentConfig.ServiceName, $ServiceInstance, $deploymentConfig.ApiAppName
    $appRegistration = Assert-CorvusAzureAdAppForAppService -AppName $appRegistrationName

    Assert-CorvusAzureAdAppRole `
        -AppObjectId $appRegistration.ObjectId `
        -AppRoleId "7619c293-764c-437b-9a8e-698a26250efd" `
        -DisplayName "Tenancy administrator" `
        -Description "Ability to create, modify, read, and remove tenants" `
        -Value "TenancyAdministrator" `
        -AllowedMemberTypes "User", "Application"

    Assert-CorvusAzureAdAppRole `
        -AppObjectId $appRegistration.ObjectId `
        -AppRoleId "60743a6a-63b6-42e5-a464-a08698a0e9ed" `
        -DisplayName "Tenancy reader" `
        -Description "Ability to read information about tenants" `
        -Value "TenancyReader" `
        -AllowedMemberTypes "User", "Application"

    $keyVaultName = $deploymentConfig.KeyVaultName
    $tenantAdminAppId = try { $deploymentConfig.TenantAdminAppId } catch { $null }
    $tenantAdminSecretName = "DefaultTenantAdminPassword"
    $defaultTenantAdminSpName = '{0}.{1}.tenantadmin' -f $StackName, $Environment

    # Ensure a default tenancy administrator principal is setup/available
    if (!$tenantAdminAppId -and !$DoNotUseGraph) {
        # look-up from AAD directly, if we have access
        $existingSp = Get-AzADServicePrincipal -DisplayName $defaultTenantAdminSpName        
        if ($existingSp) {
            $tenantAdminAppId = $existingSp.ApplicationId
        }
        else {
            Write-Host "Creating default tenancy administrator service principal"
            $newSp = New-AzADServicePrincipal -DisplayName $defaultTenantAdminSpName -SkipAssignment

            Set-AzKeyVaultSecret -VaultName $keyVaultName -Name $tenantAdminSecretName -SecretValue $newSp.Secret | Out-Null
        }
    }
    else {
        Write-Error "Unable to determine the AAD ApplicationId for default tenancy admnistrator - No graph token available and TenantAdminId not supplied"
    }

    # Read the tenant admin credentials
    $tenantAdminSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $tenantAdminSecretName

    # Add guard rails for if we've lost the keyvault secret, re-generate a new one (if we have AAD access)
    if (!$tenantAdminSecret -and !$DoNotUseGraph) {
        Write-Host "Resetting credential for default tenancy admnistrator service principal"
        $newSpCred = $existingSp | New-AzADServicePrincipalCredential
        Set-AzKeyVaultSecret -VaultName $keyVaultName -Name $tenantAdminSecretName -SecretValue $newSpCred.Secret | Out-Null
        $tenantAdminSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $tenantAdminSecretName
    }
    elseif (!$tenantAdminSecret) {
        Write-Error "The default tenancy administrator credential was not available in the keyvault - access to AAD is required to resolve this issue"
    }
    
    if (!$ApplicationDeploymentOnly) {
        Write-Host "Starting provisioning..."

        # deploy ARM template
        $templateParameters = @{
            stackName = $StackName
            serviceName = "tenancy"
            environment = $Environment
            tenancyStorageSku = $deploymentConfig.TenancyStorageSku
            appInsightsInstrumentationKey = $deploymentConfig.AppInsightsInstrumentationKey
            functionEasyAuthAadClientId = $appRegistration.ApplicationId
        }

        $rgName = "{0}.{1}.{2}-{3}" -f $StackName, $Environment, $deploymentConfig.ServiceName, $ServiceInstance
        $armResults = Invoke-CorvusArmTemplateDeployment `
            -ResourceGroupName $rgName `
            -Location $deploymentConfig.AzureLocation `
            -ArmTemplatePath "$here/deploy.json" `
            -TemplateParameters $templateParameters `
            -AdditionalArtifactsFolderPath "$here/arm-artifacts" `
            -Verbose

        $apiAppFunctionsAppName = $armResults.Outputs.functionsAppName.value
    }
    else {
        # TODO: get the functions app name
    }

    Write-Host "Starting application deployment..."

    # deploy the application ZIP packages
    $publishResults = Publish-CorvusAppServiceFromZipFile -Path (Resolve-Path $ApplicationZipPath).Path -AppServiceName $apiAppFunctionsAppName

    # Setup environment variables for marain cli
    $env:TenancyClient:TenancyServiceBaseUri = "https://{0}.azurewebsites.net/" -f $apiAppFunctionsAppName
    $env:TenancyClient:ResourceIdForMsiAuthentication = $appRegistration.ApplicationId
    $env:AzureServicesAuthConnectionString = "RunAs=App;AppId={0};TenantId={1};AppKey={2}" -f `
                                                        $tenantAdminAppId,
                                                        $AadTenantId,
                                                        (ConvertFrom-SecureString $tenantAdminSecret.SecretValue -AsPlainText)

    # Ensure the tenancy instance is initialised
    Write-Host "Initialising marain tenancy instance..."
    $scriptBlock = {  
        $cliOutput = & marain init
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Error whilst trying to initialise the marain tenancy instance: ExitCode=$LASTEXITCODE`n$cliOutput"
        }
    }
    
    Invoke-CorvusCommandWithRetry `
        -Command $scriptBlock `
        -RetryCount 5 `
        -RetryDelay 30
    
}
catch {
    Write-Warning $_.ScriptStackTrace
    Write-Warning $_.InvocationInfo.PositionMessage
    Write-Error ("Deployment error: `n{0}" -f $_.Exception.Message)
}

Write-Host "Deployment completed successfully."