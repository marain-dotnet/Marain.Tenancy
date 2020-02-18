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

    #$ServiceDeploymentContext.Variables["KeyVaultName"] = $DeploymentResult.Outputs.keyVaultName.Value
    $ServiceDeploymentContext.SetAppServiceDetails($DeploymentResult.Outputs.functionServicePrincipalId.Value)
}