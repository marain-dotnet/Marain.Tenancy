# Load domain-specific task definitions
$taskGroups = @(
    "aad"
    "provision"
    "deploy"
)
$taskGroups | ForEach-Object {
    $taskFilename = "$_.tasks.ps1"
    $taskFilePath = Resolve-Path ([IO.Path]::Combine($PSScriptRoot, "tasks", $taskFilename))
    Write-Information "Import '$taskFilePath'"
    . $taskFilePath
}

# Import the build process that orchestrates the above tasks
. (Join-Path $PSScriptRoot "tasks/deploy.process.ps1")