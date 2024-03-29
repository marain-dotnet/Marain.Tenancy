﻿<#
This is called during Marain.Instance infrastructure deployment prior to the Marain-ArmDeploy.ps
script. It is our opportunity to perform initialization that needs to complete before any Azure
resources are created.

We create the Azure AD Application that the Tenancy function will use to authenticate incoming
requests. (Currently, this application is used with Azure Easy Auth, but the application could also
use it directly.)

#>

#Requires -Modules @{ ModuleName = "Az.Resources"; ModuleVersion = "5.4.0" }

# Marain.Instance expects us to define just this one function.
Function MarainDeployment([MarainServiceDeploymentContext] $ServiceDeploymentContext) {

    $app = $ServiceDeploymentContext.DefineAzureAdAppForAppService()

    $AdminAppRoleId = "7619c293-764c-437b-9a8e-698a26250efd"
    $app.EnsureAppRolesContain(
        $AdminAppRoleId,
        "Tenancy administrator",
        "Ability to create, modify, read, and remove tenants",
        "TenancyAdministrator",
        ("User", "Application"))

    $ReaderAppRoleId = "60743a6a-63b6-42e5-a464-a08698a0e9ed"
    $app.EnsureAppRolesContain(
        $ReaderAppRoleId,
        "Tenancy reader",
        "Ability to read information about tenants",
        "TenancyReader",
        ("User", "Application"))


    $keyVaultName = $ServiceDeploymentContext.InstanceContext.KeyVaultName
    $tenantAdminSecretName = "DefaultTenantAdminPassword"
    $defaultTenantAdminSpName = '{0}{1}tenantadmin' -f $ServiceDeploymentContext.InstanceContext.Prefix, $ServiceDeploymentContext.InstanceContext.EnvironmentSuffix

    # Ensure a default tenancy administrator principal is setup/available
    if ($ServiceDeploymentContext.InstanceContext.AadAppIds.ContainsKey($defaultTenantAdminSpName)) {
        # Use AadAppId provided on the command-line
        $ServiceDeploymentContext.InstanceContext.TenantAdminAppId = $ServiceDeploymentContext.InstanceContext.AadAppIds[$defaultTenantAdminSpName]
    }
    elseif (!$ServiceDeploymentContext.InstanceContext.DoNotUseGraph) {
        # look-up from AAD directly, if we have access
        $existingSp = Get-AzADServicePrincipal -DisplayName $defaultTenantAdminSpName        
        if ($existingSp) {
            $ServiceDeploymentContext.InstanceContext.TenantAdminAppId = $existingSp.AppId
        }
        else {
            Write-Host "Creating default tenancy administrator service principal"
            $newSpParams = @{
                DisplayName = $defaultTenantAdminSpName
            }
            $newSp = New-AzADServicePrincipal @newSpParams  

            $ServiceDeploymentContext.InstanceContext.TenantAdminAppId = $newSp.AppId
            $ServiceDeploymentContext.InstanceContext.TenantAdminObjectId = $newSp.Id
            $spSecret = ConvertTo-SecureString $newSp.PasswordCredentials[0].SecretText -AsPlainText
            Set-AzKeyVaultSecret -VaultName $keyVaultName -Name $tenantAdminSecretName -SecretValue $spSecret | Out-Null
        }
    }
    else {
        Write-Error "Unable to determine the AAD ApplicationId for default tenancy admnistrator - No graph token available and '{0}' not present in AadAppIds" -f $defaultTenantAdminSpName
    }

    # Read the tenant admin credentials
    $tenantAdminSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $tenantAdminSecretName

    # Add guard rails for if we've lost the keyvault secret, re-generate a new one (if we have AAD access)
    if (!$tenantAdminSecret -and !$ServiceDeploymentContext.InstanceContext.DoNotUseGraph) {
        Write-Host "Resetting credential for default tenancy admnistrator service principal"
        $newSpCred = $existingSp | New-AzADServicePrincipalCredential
        Set-AzKeyVaultSecret -VaultName $keyVaultName -Name $tenantAdminSecretName -SecretValue (ConvertTo-SecureString $newSpCred.SecretText -AsPlainText) | Out-Null
        $tenantAdminSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $tenantAdminSecretName
    }
    elseif (!$tenantAdminSecret) {
        Write-Error "The default tenancy administrator credential was not available in the keyvault - access to AAD is required to resolve this issue"
    }

    $ServiceDeploymentContext.InstanceContext.TenantAdminSecret = ConvertFrom-SecureString $tenantAdminSecret.SecretValue -AsPlainText
}