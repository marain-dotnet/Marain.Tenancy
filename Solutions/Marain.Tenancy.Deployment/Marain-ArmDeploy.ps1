<#
This is called during Marain.Instance infrastructure deployment after the Marain-PreDeploy.ps
script. It is our opportunity to create Azure resources.
#>

# Marain.Instance expects us to define just this one function.
Function MarainDeployment([MarainServiceDeploymentContext] $ServiceDeploymentContext) {

    $TenancyAuthAppId = $ServiceDeploymentContext.GetAppId()
    $TemplateParameters = @{
        appName="tenancy"
        functionEasyAuthAadClientId=$TenancyAuthAppId
        appInsightsInstrumentationKey=$ServiceDeploymentContext.InstanceContext.ApplicationInsightsInstrumentationKey
    }
    $InstanceResourceGroupName = $InstanceDeploymentContext.MakeResourceGroupName("tenancy")
    $DeploymentResult = $ServiceDeploymentContext.InstanceContext.DeployArmTemplate(
        $PSScriptRoot,
        "deploy.json",
        $TemplateParameters,
        $InstanceResourceGroupName)

    $ServiceDeploymentContext.SetAppServiceDetails($DeploymentResult.Outputs.functionServicePrincipalId.Value)

    
    # Setup environment variables for marain cli
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