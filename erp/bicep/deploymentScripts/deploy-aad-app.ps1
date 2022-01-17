[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string] $DisplayName,

    [string[]] $ReplyUrls = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 4.0

# Workaround current deployScripts limitation only available with v6.6
# We want to use 7.x to take advantage of MS Graph (rather than the deprecated Azure Graph)
# Update-Module Az.Resources -Verbose -Force

$azCtx = Get-AzContext
$azCtx | Format-List | Out-String | Write-Host

Write-Host "Checking for app '$DisplayName'"
$app = Get-AzADApplication -DisplayNameStartWith $DisplayName |
            Where-Object {$_.DisplayName -eq $DisplayName}

# NOTE: How should we handle finding multiple matching apps? (display names need not be unique)

if ($app) {
    Write-Host "Found existing app with AppId $($app.ApplicationId)"
    $replyUrlsOk = $true
    ForEach ($ReplyUrl in $ReplyUrls) {
        if (-not $app.ReplyUrls.Contains($ReplyUrl)) {
            $replyUrlsOk = $false
            Write-Host "Reply URL $ReplyUrl not present in app"
        }
    }

    if (-not $replyUrlsOk) {
        Write-Host "Updating reply URLs: $replyUrls"
        $app = Update-AzADApplication -ObjectId $app.ObjectId `
                                      -ReplyUrl $ReplyUrls
    }
} else {
    Write-Host "Creating new app"
    $createParams = @{ DisplayName = $DisplayName }
    if ($ReplyUrls.Count -gt 0) {
        $createParams += @{ ReplyUrls = $ReplyUrls }
    }
    Write-Verbose "createParams:`n$($createParams | ConvertTo-Json)"

    $app = New-AzADApplication @createParams
    Write-Host "Created new app with AppId $($app.ApplicationId)"
}

Write-Verbose "Application Details:`n$($app | ConvertTo-Json)"

# Setup ARM outputs
$DeploymentScriptOutputs = @{
    applicationId = $app.ApplicationId
    objectId = $app.ObjectId
}
