<#
This is called during Marain.Instance infrastructure deployment prior to the Marain-ArmDeploy.ps
script. It is our opportunity to perform initialization that needs to complete before any Azure
resources are created.

We create the Azure AD Application that the Tenancy function will use to authenticate incoming
requests. (Currently, this application is used with Azure Easy Auth, but the application could also
use it directly.)

#>

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
    # Ensure a default tenancy administrator principal is setup
    $defaultTenantAdminSpName = '{0}{1}tenantadmin' -f $ServiceDeploymentContext.InstanceContext.Prefix, $ServiceDeploymentContext.InstanceContext.EnvironmentSuffix
    $existingSp = Get-AzADServicePrincipal -DisplayName $defaultTenantAdminSpName
    if (!$ServiceDeploymentContext.InstanceContext.DoNotUseGraph -and !$existingSp) {
        Write-Host "Setting-up default tenant administrator"
        $newSp = & az ad sp create-for-rbac -n $defaultTenantAdminSpName --skip-assignment
        if ($LASTEXITCODE -eq 0) {
            $newSpObj = $newSp | ConvertFrom-Json
            try {                
                $ServiceDeploymentContext.InstanceContext.TenantAdminAppId = $newSpObj.appId
                $ServiceDeploymentContext.InstanceContext.TenantAdminObjectId = (Get-AzADServicePrincipal -ApplicationId $newSpObj.appId).Id
                $ServiceDeploymentContext.InstanceContext.TenantAdminSecret = $newSpObj.password
                Set-AzKeyVaultSecret -VaultName $keyVaultName -Name $tenantAdminSecretName -SecretValue (ConvertTo-SecureString $newSpObj.password -AsPlainText -Force)
            }
            catch {
                # we've not saved the secret yet and we'll lose it now, so get rid of the SP so we can try again next time
                & az ad app delete --id $newSpObj.appId
                throw $_
            }
        }
        else {
            Write-Error "The azure-cli returned non-zero status whilst creating the default tenant administrator principal - check previous errors"
        }
    }
    elseif (!$existingSp) {
        Write-Error "No graph token available - full access is required for at least the first deployment"
    }
    else {
        # Read the existing tenant admin details
        $ServiceDeploymentContext.InstanceContext.TenantAdminAppId = $existingSp.ApplicationId
        $ServiceDeploymentContext.InstanceContext.TenantAdminObjectId = (Get-AzADServicePrincipal -ApplicationId $existingSp.ApplicationId).Id
        $ServiceDeploymentContext.InstanceContext.TenantAdminSecret = (Get-AzKeyVaultSecret -VaultName $keyVaultName -Name $tenantAdminSecretName).SecretValueText
    }

}