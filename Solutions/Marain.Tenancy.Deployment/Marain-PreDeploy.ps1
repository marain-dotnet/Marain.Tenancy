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
}