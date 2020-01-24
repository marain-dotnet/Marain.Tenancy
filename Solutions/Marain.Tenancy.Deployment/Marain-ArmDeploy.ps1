<#
This is called during Marain.Instance infrastructure deployment after the Marain-PreDeploy.ps
script. It is our opportunity to create Azure resources.
#>

# Marain.Instance expects us to define just this one function.
Function MarainDeployment([MarainServiceDeploymentContext] $ServiceDeploymentContext) {

	# TODO: deployment package folder should be in shared infrastructure storage account. We
	# don't want to be creating a storage account per service just to support this.
	$TemplateParameters = @{
		appName="tenancy"
		functionsAppPackageFolder="MsDeploy"
		functionsAppPackageFileName="Marain.Tenancy.Host.Functions.zip"
		functionEasyAuthAadClientId=$ServiceDeploymentContext.Variables["TenancyAppId"]
	}
	$InstanceResourceGroupName = $InstanceDeploymentContext.MakeResourceGroupName("tenancy")
	$ServiceDeploymentContext.InstanceContext.DeployArmTemplate(
		$PSScriptRoot,
		"deploy.json",
		$TemplateParameters,
		$InstanceResourceGroupName)

	# TODO: is there anything that comes (or should come) out of the deployment that we need
	# to feed in anywhere else? E.g., we can't know the Managed Identity identifiers until
	# after ARM deployment completes, and it may be necessary to add the relevant service
	# principals to app roles in some cases (although probably not for tenancy).

	# Write-Host 'Granting KV secret access to current user'

	# .\Scripts\Grant-CurrentAadUserKeyVaultSecretAccess `
	# 	-ResourceGroupName $ResourceGroupName `
	# 	-KeyVaultName $KeyVaultName

	# .\Scripts\Add-CosmosAccessKeyToKeyVault `
	# 	-ResourceGroupName $ResourceGroupName `
	# 	-KeyVaultName $KeyVaultName `
	# 	-CosmosDbName $CosmosDbName `
	# 	-SecretName 'tenancystorecosmosdbkey'

	# if ($IsDeveloperEnvironment) {
	# 	Write-Host 'Skipping function app access grants because we are deploying a developer environment'
	# } else {
	# 	Write-Host 'Grant the function access to the KV'

	# 	$FunctionAppName = $Prefix + $AppName.ToLower() + $Suffix

	# 	.\Scripts\Grant-KeyVaultSecretGetToMsi `
	# 		-ResourceGroupName $ResourceGroupName `
	# 		-KeyVaultName $KeyVaultName `
	# 		-AppName $FunctionAppName

	# 	Write-Host 'Revoking KV secret access to current user'

	# 	.\Scripts\Revoke-CurrentAadUserKeyVaultSecretAccess `
	# 		-ResourceGroupName $ResourceGroupName `
	# 		-KeyVaultName $KeyVaultName
	# }

	# if ($UpdateLocalConfigFiles) {
	# 	Write-Host 'Updating local.settings.json files'

	# 	.\Scripts\Update-LocalConfigFiles.ps1 `
	# 		-ResourceGroupName $ResourceGroupName `
	# 		-KeyVaultName $KeyVaultName `
	# 		-CosmosDbName $CosmosDbName `
	# 		-AppInsightsName $DefaultName
	# 	}
}