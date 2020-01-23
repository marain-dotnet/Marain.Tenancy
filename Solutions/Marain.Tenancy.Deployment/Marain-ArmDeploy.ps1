<#
This is called during Marain.Instance infrastructure deployment prior to the Marain-ArmDeploy.ps
script. It is our opportunity to perform initialization that needs to complete before any Azure
resources are created.

We create the Azure AD Application that the Tenancy function will use to authenticate incoming
requests. (Currently, this application is used with Azure Easy Auth, but the application could also
use it directly.)

#>
# Marain.Instance expects us to define just this one function.
Function MarainDeployment([MarainInstanceDeploymentContext] $deployContext) {

# [CmdletBinding(DefaultParametersetName='None')] 
# param(
#     [string] $Prefix = "mar",
# 	[string] $AppName = "tenancy",
# 	[ValidateLength(3,12)]
# 	[string] $Suffix = "dev",
# 	[string] $FunctionsMsDeployPackagePath = "..\Marain.Tenancy.Host.Functions\bin\Release\package\Marain.Tenancy.Host.Functions.zip",	
# 	[string] $ResourceGroupLocation = "northeurope",
# 	[string] $ArtifactStagingDirectory = ".",
# 	[string] $ArtifactStorageContainerName = "stageartifacts",
# 	[switch] $IsDeveloperEnvironment,
# 	[switch] $UpdateLocalConfigFiles,
# 	[switch] $SkipDeployment
# )

# Begin{
# 	# Setup options and variables
# 	$ErrorActionPreference = 'Stop'
	# TODO: does this actually work when we've been dot sourced?

	Set-Location $PSScriptRoot

	# $Suffix = $Suffix.ToLower()
	# $AppName  = $AppName.ToLower()
	# $Prefix = $Prefix.ToLower()

	$ResourceGroupName = $deployContext.Prefix + "." + $deployContext.Suffix + "." + $deployContext.AppName
	$DefaultName = $Prefix + $AppName.ToLower() + $Suffix

	# $ArtifactStorageResourceGroupName = $ResourceGroupName + ".artifacts";
	# $ArtifactStorageAccountName = $Prefix + $AppName + $Suffix + "ar"
	# $ArtifactStagingDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, $ArtifactStagingDirectory))

	# $FunctionsMsDeployPackageFolderName = "MsDeploy";

	# $FunctionsAppPackageFileName = [System.IO.Path]::GetFileName($FunctionsMsDeployPackagePath)

	# $CosmosDbName = $DefaultName
	# $KeyVaultName = $DefaultName

		# # Create resource group and artifact storage account
		# Write-Host "`nStep1: Creating resource group $ResourceGroupName and artifact storage account $ArtifactStorageAccountName" -ForegroundColor Green
		# try {
		# 	.\Scripts\Create-StorageAccount.ps1 `
		# 		-ResourceGroupName $ArtifactStorageResourceGroupName `
		# 		-ResourceGroupLocation $ResourceGroupLocation `
		# 		-StorageAccountName $ArtifactStorageAccountName
		# }
		# catch{
		# 	throw $_
		# }

		# # Copy msbuild package to artifact directory
		# if ($IsDeveloperEnvironment) {
		# 	Write-Host "`nStep2: Skipping function msdeploy package copy as we are deploying a developer environment only"  -ForegroundColor Green
		# } else {
		# 	Write-Host "`nStep2: Coping functions msdeploy packages to artifact directory $FunctionsMsDeployPackageFolderName"  -ForegroundColor Green
		# 	try {
		# 		Copy-Item -Path $FunctionsMsDeployPackagePath -Destination (New-Item -Type directory -Force "$FunctionsMsDeployPackageFolderName") -Force -Recurse
		# 	}
		# 	catch{
		# 		throw $_
		# 	}
		# }

	# Deploy main ARM template
	Write-Host "`nStep4: Deploying main resources template"  -ForegroundColor Green
	try{
		$parameters = New-Object -TypeName Hashtable

		$parameters["prefix"] = $Prefix
		$parameters["appName"] = $AppName
		$parameters["environment"] = $Suffix

		$parameters["functionEasyAuthAadClientId"] = $EasyAuthAppAd.AppId

		$parameters["functionsAppPackageFileName"] = $FunctionsAppPackageFileName

		$parameters["functionsAppPackageFolder"] = $FunctionsMsDeployPackageFolderName

		$parameters["isDeveloperEnvironment"] = $IsDeveloperEnvironment.IsPresent

		$TemplateFilePath = [System.IO.Path]::Combine($ArtifactStagingDirectory, "deploy.json")

		$str = $parameters | Out-String
		Write-Host $str

		Write-Host $ArtifactStagingDirectory

		$deploymentResult = .\Deploy-AzureResourceGroup.ps1 `
			-UploadArtifacts `
			-ResourceGroupLocation $ResourceGroupLocation `
			-ResourceGroupName $ResourceGroupName `
			-StorageAccountName $ArtifactStorageAccountName `
			-ArtifactStagingDirectory $ArtifactStagingDirectory `
			-StorageContainerName $ArtifactStorageContainerName `
			-TemplateParameters $parameters `
			-TemplateFile $TemplateFilePath
	}
	catch{
		throw $_
	}

	Write-Host "`nStep 5: Applying configuration"

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