# <copyright file="Install-DotNetTool.ps1" company="Endjin Limited">
# Copyright (c) Endjin Limited. All rights reserved.
# </copyright>

<#
.SYNOPSIS
    Simple wrapper to install a .Net global tool if it is not already installed.
.DESCRIPTION
    Simple wrapper to install a .Net Global Tool if it is not already installed.  Any existing
    installed version will be uninstalled before installing the required version.
.EXAMPLE
    PS C:\> <example usage>
    Explanation of what the example does
.PARAMETER Name
    The name of the .Net global tool to install
.PARAMETER Version
    The version of the global tool to install.
.PARAMETER Global
    When true (the default), the tool is installed for the current user.
#>
function Install-DotNetTool
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory=$true)]
        [string] $Name,

        [Parameter()]
        [string] $Version,
        
        [Parameter()]
        [bool] $Global = $true
    )

    $allInstalledTools = & dotnet tool list -g
    $existingInstall = $allInstalledTools | select-string $Name

    if ($existingInstall -and $Version) {
        # parse out the currently installed version from the whitespace delimited columns
        $existingVersion = ($existingInstall.Line -split "[\s*]" | ? { $_ })[1]
        if ($existingVersion -ne $Version) {
            # uninstall the incorrect version
            & dotnet tool uninstall -g $Name
            # force a re-install below
            $existingInstall = $null
        }
    } 

    # Install the tool, if necessary
    if (!$existingInstall) {
        if ($Version) {
            & dotnet tool install -g $Name --version $Version
        }
        else {
            & dotnet tool install -g $Name
        }
    }

    # Ensure .NET global tools are available in the PATH environment variable
    if ($IsWindows) {
        $toolsPath = Join-Path $env:USERPROFILE ".dotnet/tools"
    }
    else {
        $toolsPath = Join-Path $env:HOME ".dotnet/tools"
    }
    if ($toolsPath -notin ($env:PATH -split [IO.Path]::PathSeparator)) {
        $env:PATH = "{0}{1}{2}" -f $env:PATH, [IO.Path]::PathSeparator, $toolsPath
    }
}
