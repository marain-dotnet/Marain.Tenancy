﻿<#
This is called during Marain.Instance infrastructure deployment prior to the Marain-ArmDeploy.ps
script. It is our opportunity to perform initialization that needs to complete before any Azure
resources are created.

We create the Azure AD Application that the Tenancy function will use to authenticate incoming
requests. (Currently, this application is used with Azure Easy Auth, but the application could also
use it directly.)

#>

# Marain.Instance expects us to define just this one function.
Function MarainDeployment([MarainServiceDeploymentContext] $ServiceDeploymentContext) {

    $app = $ServiceDeploymentContext.DefineAzureAdAppForAppService(
        "",
         "TenancyAppId")

    $AdminAppRoleId = "7619c293-764c-437b-9a8e-698a26250efd"
    $app.EnsureAppRolesContain(
        $AdminAppRoleId,
        "Tenancy administrator",
        "Full control over definition of claim permissions and rule sets",
        "TenancyAdministrator",
        ("User", "Application"))
}