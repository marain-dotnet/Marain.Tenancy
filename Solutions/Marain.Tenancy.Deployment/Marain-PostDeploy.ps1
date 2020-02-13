<#
This is called during Marain.Instance infrastructure deployment after the Marain-ArmDeploy.ps
script. It is our opportunity to do any deployment work that needs to happen after Azure resources
have been deployed.
#>

# Marain.Instance expects us to define just this one function.
Function MarainDeployment([MarainServiceDeploymentContext] $ServiceDeploymentContext) {

    # Now being set by ARM template
    #Write-Host 'Grant the function access to the KV'

    #$InstanceResourceGroupName = $InstanceDeploymentContext.MakeResourceGroupName("tenancy")
    #$KeyVaultName = $ServiceDeploymentContext.Variables["KeyVaultName"]
    #$PrincipalId = $ServiceDeploymentContext.Variables["FunctionServicePrincipalId"]
    #
    #Set-AzKeyVaultAccessPolicy `
    #    -VaultName $KeyVaultName `
    #    -ResourceGroupName $InstanceResourceGroupName `
    #    -ObjectId $PrincipalId `
    #    -PermissionsToSecrets Get

    $ServiceDeploymentContext.UploadReleaseAssetAsAppServiceSitePackage(
        "Marain.Tenancy.Host.Functions.zip",
        $ServiceDeploymentContext.AppName
    )

}