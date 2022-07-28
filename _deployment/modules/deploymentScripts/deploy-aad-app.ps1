[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string] $DisplayName,

    [string[]] $ReplyUrls = @(),

    [string[]] $IdentifierUris = @(),

    $CorvusModulePath = $null
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 4.0

<########>

function Assert-RequiredResourceAccessContains
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true)]
        [Microsoft.Azure.PowerShell.Cmdlets.Resources.MSGraph.Models.ApiV10.MicrosoftGraphApplication] $App,

        [Parameter(Mandatory=$true)]
        [string] $ResourceId,

        [Parameter(Mandatory=$true)]
        [hashtable[]] $AccessRequirements,

        [Parameter()]
        [switch] $UseAzureAdGraph
    )

    $madeChange = $false
    [array]$requiredResourceAccess = $App.requiredResourceAccess
    $resourceEntry = $requiredResourceAccess | Where-Object {$_.resourceAppId -eq $ResourceId }
    if (-not $resourceEntry) {
        $madeChange = $true
        $resourceEntry = @{resourceAppId=$ResourceId;resourceAccess=@()}
        $requiredResourceAccess += $resourceEntry
    }
    
    foreach ($access in $AccessRequirements) {
        $RequiredAccess = $resourceEntry.resourceAccess| Where-Object {$_.id -eq $access.Id -and $_.type -eq $access.Type}
        if (-not $RequiredAccess) {
            Write-Host "Adding '$ResourceId : $($access.id)' required resource access"
    
            $RequiredAccess = @{id=$access.Id; type=$access.Type}
            $resourceEntry.resourceAccess += $RequiredAccess
            $madeChange = $true
        }
    }

    if ($madeChange) {
        Write-Host "Applying updates to application"
        $App | Update-AzADApplication -RequiredResourceAccess $requiredResourceAccess

        $App = Get-AzADApplication -ObjectId $App.Id
    }

    return $App
}

function Assert-AppRole
{
    [CmdletBinding()]
    param 
    (
        [Microsoft.Azure.PowerShell.Cmdlets.Resources.MSGraph.Models.ApiV10.MicrosoftGraphApplication] $App,
        [string] $Id,
        [string] $DisplayName,
        [string] $Description,
        [string] $Value,
        [bool] $IsEnabled,
        [string[]] $AllowedMemberType
    )

    $appRoles = $App.AppRole
    $existingAppRole = $appRoles | ? { $_.Id -eq $Id }
    if (!$existingAppRole) {
        Write-Host "Adding AppRole '$DisplayName' to application '$($App.DisplayName)' [AppId=$($App.AppId)]"
        $appRoles += @{
            DisplayName = $DisplayName
            Id = $Id
            IsEnabled = $IsEnabled
            Description = $Description
            Value = $Value
            AllowedMemberType = $AllowedMemberType
        }
        $App | Update-AzADApplication -AppRole $appRoles

        $App = Get-AzADApplication -ObjectId $App.Id
    }

    return $App
}

function Assert-AppRoleAssignment
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string] $AppRoleId,

        [Parameter(Mandatory=$true)]
        [ValidateSet("user","group","servicePrincipal")]
        [string] $ClientIdentityType,

        [Parameter(Mandatory=$true)]
        [string] $ClientIdentityObjectId,

        [Parameter(Mandatory=$true)]
        [string] $TargetAccessControlServicePrincipalId
    )

    $uri = "https://graph.microsoft.com/v1.0/$($ClientIdentityType)s/$ClientIdentityObjectId/appRoleAssignments"
    
    Write-Host "Searching existing AppRole assignments for the $ClientIdentityType '$ClientIdentityObjectId'"
    $resp = Invoke-AzRestMethod -Uri $uri
    Write-Host "appRoleAssignments: StatusCode=$($resp.StatusCode)"

    if ($resp.StatusCode -ge 400) {
        throw "The was a problem retrieving the $ClientIdentityType with objectId '$ClientIdentityObjectId' [StatusCode=$($resp.StatusCode)]"
    }

    $existing =  $resp.Content | 
                    ConvertFrom-Json -Depth 100 |
                    Select-Object -ExpandProperty value |
                    ? { 
                        $_.principalId -eq $ClientIdentityObjectId -and `
                        $_.appRoleId -eq $AppRoleId -and `
                        $_.resourceId -eq $TargetAccessControlServicePrincipalId
                    }
    
    if (!$existing) {
        Write-Host "Assigning the AppRole '$AppRoleId' to the $ClientIdentityType '$ClientIdentityObjectId'"
        $body = @{
            principalId = $ClientIdentityObjectId
            resourceId = $TargetAccessControlServicePrincipalId
            appRoleId = $AppRoleId
        }

        $msGraphSp = Get-AzADServicePrincipal -ApplicationId "00000003-0000-0000-c000-000000000000"
        $uri2 = "https://graph.microsoft.com/v1.0/servicePrincipals/$($msGraphSp.Id)/appRoleAssignedTo"
        $resp = Invoke-AzRestMethod -Uri $uri2 `
                                    -Method POST `
                                    -Payload ($body | ConvertTo-Json -Depth 100 -Compress)
    }
}


<########>

$azCtx = Get-AzContext
$azCtx | Format-List | Out-String | Write-Host

$identifierUri = "api://$($azCtx.Tenant.Id)/$($DisplayName.Replace(" ","-").ToLower())"

Write-Host "Checking for app '$DisplayName' [IdentifierUri=$identifierUri]"
$app = Get-AzADApplication -DisplayNameStartWith $DisplayName |
            Where-Object { $_.IdentifierUri -eq $identifierUri }

Get-Module Az.Resources | ft | out-string | write-host

$requiredResourceAccess = @(
    @{
        ResourceAccess = @(
            @{
                Id = "d4f74230-7f32-4552-97a7-d9e5fdc4a917"     # User.Read
                Type = "Scope"
            }
        )
        ResourceAppId = "00000003-0000-0000-c000-000000000000"
    }
)

$requiredAppRoles = @(
    @{
        DisplayName = "Tenancy administrator"
        Id = "7619c293-764c-437b-9a8e-698a26250efd"
        IsEnabled = $true
        Description = "Ability to create, modify, read, and remove tenants"
        Value = "TenancyAdministrator"
        AllowedMemberType = @("User", "Application")
    }
    @{
        DisplayName = "Tenancy reader"
        Id = "60743a6a-63b6-42e5-a464-a08698a0e9ed"
        IsEnabled = $true
        Description = "Ability to read information about tenants"
        Value = "TenancyReader"
        AllowedMemberType = @("User", "Application")
    }
)

if ($app) {
    Write-Host "Found existing app with AppId $($app.Id)"
    
    $idUrisOk = $true
    ForEach ($idUri in $IdentifierUris) {
        if (-not $app.IdentifierUri.Contains($idUri)) {
            $idUrisOk = $false
            Write-Host "Identifier URI $idUri not present in app"
        }
    }
    if (-not $idUrisOk) {
        Write-Host "Updating Identifier URIs: $IdentifierUris"
        $app | Update-AzADApplication -IdentifierUri $IdentifierUris -AvailableToOtherTenants
    }

    $replyUrlsOk = $true
    ForEach ($ReplyUrl in $ReplyUrls) {
        if (-not $app.web.RedirectUri.Contains($ReplyUrl)) {
            $replyUrlsOk = $false
            Write-Host "Reply URL $ReplyUrl not present in app"
        }
    }

    if (-not $replyUrlsOk) {
        Write-Host "Updating reply URLs: $replyUrls"
        $app | Update-AzADApplication -ReplyUrl $ReplyUrls
    }

    Write-Host "Ensuring app's required resources"
    $app = Assert-RequiredResourceAccessContains -App $app `
                                          -ResourceId "00000003-0000-0000-c000-000000000000" `
                                          -AccessRequirements @(
                                                                @{
                                                                    Id = "d4f74230-7f32-4552-97a7-d9e5fdc4a917"     # User.Read
                                                                    Type = "Scope"
                                                                }
                                                            )

    Write-Host "Ensuring app roles"
    $requiredAppRoles | % {
        $app = Assert-AppRole -App $app @_
    }

} else {
    Write-Host "Creating new app"
    $createParams = @{
        DisplayName = $DisplayName
        IdentifierUri = $identifierUri
        RequiredResourceAccess = $requiredResourceAccess
        AppRole = $requiredAppRoles
    }
    if ($ReplyUrls.Count -gt 0) {
        $createParams += @{ web = @{ redirectUris = $ReplyUrls } }
    }
    Write-Verbose "createParams:`n$($createParams | ConvertTo-Json -Depth 100)"

    $app = New-AzADApplication @createParams
    Write-Host "Created new app with AppId $($app.AppId) [IdentifierUri=$($app.IdentifierUri)]"
}

Write-Verbose "Application Details:`n$($app | ConvertTo-Json -Depth 100)"


# placeholder approle assignment
$appSp = Get-AzADServicePrincipal -ApplicationId $App.AppId
if (!$appSp) {
    $appSp = New-AzADServicePrincipal -ApplicationId $App.AppId -DisplayName $App.DisplayName
}
Assert-AppRoleAssignment -AppRoleId "60743a6a-63b6-42e5-a464-a08698a0e9ed" `
                         -ClientIdentityType "user" `
                         -ClientIdentityObjectId "f3498fd9-cff0-44a9-991c-c017f481adf0" `
                         -TargetAccessControlServicePrincipalId $appSp.Id

# Setup ARM outputs
$DeploymentScriptOutputs = @{
    applicationId = $app.AppId
    objectId = $app.Id
}
