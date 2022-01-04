function createAzureAdAppForAppService {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string] $AppName,

        [Parameter(Mandatory=$true)]
        [string] $KeyVaultName,

        [Parameter(Mandatory=$true)]
        [string] $KeyVaultSecretName
    )

    $app = Invoke-CorvusAzCli "ad app list --all --query `"[?displayName == '$appName']`"" -AsJson

    # TODO: Add support for resetting SP secret if one isn't available via key vault
    if (!$app) {
        Write-Information "Creating AAD application {$appName}"
        $createArgs = @(
            "ad app create --display-name $appName"
            # AAD has added validation to id-uris
            # ref: https://docs.microsoft.com/en-us/azure/active-directory/develop/reference-breaking-changes#appid-uri-in-single-tenant-applications-will-require-use-of-default-scheme-or-verified-domains
            # "--identifier-uris https://$appName.azurecontainerapps.io/"
            # "--reply-urls https://$appName.azurecontainerapps.io/.auth/login/aad/callback"
        )
        $app = Invoke-CorvusAzCli $createArgs -AsJson

        $sp = Invoke-CorvusAzCli "ad sp create-for-rbac -n $appName --skip-assignment" -AsJson
        Write-Information "Storing AAD service principal credentials in key vault {$keyVaultName}/{$KeyVaultSecretName}"
        Set-AzKeyVaultSecret -VaultName $keyVaultName `
                             -Name $KeyVaultSecretName `
                             -ContentType "application/json" `
                             -SecretValue ($sp | ConvertTo-Json -Compress | ConvertTo-SecureString -AsPlainText -Force) `
            | Out-Null
    }
    else {
        Write-Information "AAD application {$appName} already configured"
    }

    return $app
}

function setAppRoles {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string] $Id,
        [Parameter(Mandatory=$true)]
        [array] $AppRoles
    )
    Write-InformationLog "Configuring AAD application roles: {$($AppRoles.value -join ',')}"
    $manifest = New-TemporaryFile
    try {
        $AppRoles | ConvertTo-Json -Depth 100 | Set-Content -Path $manifest
        Get-Content $manifest -Raw | Write-Verbose
        Invoke-CorvusAzCli "ad app update --id $Id --app-roles `"@$manifest`""
    }
    finally {
        Remove-Item $manifest
    }
}

task DeployAzureAdApplications {

    foreach ($appToDeploy in $azureAdApplicationsToDeploy) {

        $app = createAzureAdAppForAppService `
                                -AppName $appToDeploy.Name `
                                -KeyVaultName $keyVaultName `
                                -KeyVaultSecretName $appToDeploy.KeyVaultSecretName
                            
        if ($appToDeploy.Roles -and $appToDeploy.Roles.Count -gt 0) {
            $app = setAppRoles -Id $app.objectId `
                               -AppRoles $appToDeploy.Roles
        }

        $app | ConvertTo-Json | Write-Verbose
    }
}
