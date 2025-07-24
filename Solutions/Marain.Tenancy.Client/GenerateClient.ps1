$InputFile = (Join-Path $PSScriptRoot "..\Marain.Tenancy.OpenApi.Service\Marain\Tenancy\OpenApi\TenancyServices.yaml")
$OutputFolder = (Join-Path $PSScriptRoot "Marain\Tenancy\Client\Internal").Replace("\", "/")    # because apparently they don't test autorest on Windows these days

# If you do not have Kiota, install it with:
#   dotnet tool install --global Microsoft.OpenApi.Kiota
kiota generate -l CSharp -c MarainTenancyClient -n Marain.Tenancy.Client.Internal -d $InputFile -o $OutputFolder