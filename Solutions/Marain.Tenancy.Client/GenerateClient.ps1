
# This requires the Tenancy function to be running locally on its default port of 7071
$tmp = (New-TemporaryFile).FullName

Write-Output "Downloading Swagger from local API instance to " $tmp

Invoke-WebRequest http://localhost:7071/api/swagger -o $tmp

$OutputFolder = (Join-Path $PSScriptRoot "Marain\Tenancy\Client\").Replace("\", "/")    # because apparently they don't test autorest on Windows these days

# If you do not have autorest, install it with:
#   npm install -g autorest
# Ensure it is up to date with
#   autorest --latest
autorest --input-file=$tmp --csharp --output-folder=$OutputFolder --namespace=Marain.Tenancy.Client --add-credentials