﻿<#
This is called during Marain.Instance infrastructure deployment after the Marain-ArmDeploy.ps
script. It is our opportunity to do any deployment work that needs to happen after Azure resources
have been deployed.
#>

# Marain.Instance expects us to define just this one function.
Function MarainDeployment([MarainServiceDeploymentContext] $ServiceDeploymentContext) {

    #
    # NOTE: This is not currently required, the vanilla principle can use the CLI without additional perms
    #
    # Write-Host "Assigning admin spn (used for tenancy registration) to the tenancy service app's admin role"

    # $TenancyAdminAppRoleId = "7619c293-764c-437b-9a8e-698a26250efd"
    # $TargetAppId = $ServiceDeploymentContext.AppServices[$ServiceDeploymentContext.AppName].AuthAppId
    # $TargetSp = Get-AzADServicePrincipal -ApplicationId $TargetAppId

    # $body = @{
    #     appRoleId = $TenancyAdminAppRoleId
    #     principalId = $ServiceDeploymentContext.InstanceContext.TenantAdminObjectId
    #     resourceId = $TargetSp.Id
    # }
    # az rest --method post `
    #     --uri https://graph.microsoft.com/beta/servicePrincipals/$($ServiceDeploymentContext.InstanceContext.TenantAdminObjectId)/appRoleAssignments `
    #     --body ($body | ConvertTo-Json -Compress) `
    #     --headers "Content-Type=application/json"

    # Setup enviornment variables for marain cli
    $env:TenancyClient:TenancyServiceBaseUri = "https://{0}.azurewebsites.net/" -f $ServiceDeploymentContext.AppName
    $env:TenancyClient:ResourceIdForMsiAuthentication = $ServiceDeploymentContext.AppServices[$ServiceDeploymentContext.AppName].AuthAppId
    $env:AzureServicesAuthConnectionString = "RunAs=App;AppId={0};TenantId={1};AppKey={2}" -f `
                                                        $ServiceDeploymentContext.InstanceContext.TenantAdminAppId,
                                                        $ServiceDeploymentContext.InstanceContext.TenantId,
                                                        $ServiceDeploymentContext.InstanceContext.TenantAdminSecret

    # Ensure the tenancy instance is initialised
    try {
        Write-Host "Initialising marain tenancy instance..."
        $cliOutput = & $ServiceDeploymentContext.InstanceContext.MarainCliPath init
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Error whilst trying to initialise the marain tenancy instance: ExitCode=$LASTEXITCODE`n$cliOutput"
        }
    }
    catch {
        throw $_
    }
}