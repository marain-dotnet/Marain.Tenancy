<#

.EXAMPLE

.\deploy.ps1 `
	-Prefix "mar" `
	-AppName "tenancy" `
	-Environment "dev" `
	-FunctionsMsDeployPackagePath "..\Marain.Tenancy.Host.Functions\bin\Release\package\Marain.Tenancy.Host.Functions.zip"
#>

[CmdletBinding(DefaultParametersetName='None')] 
param(
    [string] $Prefix = "mar",
	[string] $AppName = "tenancy",
	[ValidateLength(3,12)]
	[string] $Suffix = "dev",
	[string] $FunctionsMsDeployPackagePath = "..\Marain.Tenancy.Host.Functions\bin\Release\package\Marain.Tenancy.Host.Functions.zip",	
	[string] $ResourceGroupLocation = "northeurope",
	[string] $ArtifactStagingDirectory = ".",
	[string] $ArtifactStorageContainerName = "stageartifacts",
	[switch] $IsDeveloperEnvironment,
	[switch] $UpdateLocalConfigFiles,
	[switch] $SkipDeployment
)

Begin{
	# Setup options and variables
	$ErrorActionPreference = 'Stop'
	Set-Location $PSScriptRoot

	$Suffix = $Suffix.ToLower()
	$AppName  = $AppName.ToLower()
	$Prefix = $Prefix.ToLower()

	$ResourceGroupName = $Prefix + "." + $AppName.ToLower() + "." + $Suffix
	$DefaultName = $Prefix + $AppName.ToLower() + $Suffix

	$ArtifactStorageResourceGroupName = $ResourceGroupName + ".artifacts";
	$ArtifactStorageAccountName = $Prefix + $AppName + $Suffix + "ar"
	$ArtifactStagingDirectory = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($PSScriptRoot, $ArtifactStagingDirectory))

	$FunctionsMsDeployPackageFolderName = "MsDeploy";

	$FunctionsAppPackageFileName = [System.IO.Path]::GetFileName($FunctionsMsDeployPackagePath)

	$CosmosDbName = $DefaultName
	$KeyVaultName = $DefaultName
}

Process{

	#.\Scripts\Grant-CurrentAadUserKeyVaultSecretAccess `
	#	-ResourceGroupName $ResourceGroupName `
	#	-KeyVaultName $KeyVaultName

	.\Scripts\Add-CosmosAccessKeyToKeyVault `
		-ResourceGroupName $ResourceGroupName `
		-KeyVaultName $KeyVaultName `
		-CosmosDbName $CosmosDbName `
		-SecretName 'tenancystorecosmosdbkey'

	Write-Host 'Grant the function access to the KV'

	$FunctionAppName = $Prefix + $AppName.ToLower() + $Suffix

	.\Scripts\Grant-KeyVaultSecretGetToMsi `
		-ResourceGroupName $ResourceGroupName `
		-KeyVaultName $KeyVaultName `
		-AppName $FunctionAppName

	Write-Host 'Revoking KV secret access to current user'

	.\Scripts\Revoke-CurrentAadUserKeyVaultSecretAccess `
		-ResourceGroupName $ResourceGroupName `
		-KeyVaultName $KeyVaultName
}

End{
	Write-Host -ForegroundColor Green "`n######################################################################`n"
	Write-Host -ForegroundColor Green "Deployment finished"
	Write-Host -ForegroundColor Green "`n######################################################################`n"
}

