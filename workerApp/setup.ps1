$ErrorActionPreference = "Stop"
Set-StrictMode -Version 4.0

$here = Split-Path -Parent $PSCommandPath


$RESOURCEGROUP = "rg-marain-workerapps-spike"                                   # All the resources would be deployed in this resource group
$RESOURCEGROUP_LOCATION = "centralus"                                           # Location for non container app components
$ENVNAME = "endmarainspike"                                                     # Name of the WorkerApps Environment
$APP_NAME = "marain-tenancy"                                                    # Name of the WorkerApp
$LOCATION = "centraluseuap"                                                     # Location for container app components
$STORAGENAME = "$($ENVNAME)sa"                                                  # Tenancy storage account
$LOGSNAME="${ENVNAME}logs"                                                      # log analytics workspace used for app container logging
$AINAME="${ENVNAME}appinsights"                                                 # appinsights used for app container logging
$VAULTNAME = "$($ENVNAME)kv"                                                    # Tenancy key vault
$ACRNAME = "$($ENVNAME)acr"                                                     # Private container registry used to host app container images
$APP_IMAGE = "$ACRNAME.azurecr.io/marain/tenancy-service:latest"                # Container image for the app


az cloud set --name AzureCloud
az login
az account set -s "Microsoft Azure Sponsorship"

az group create --name $RESOURCEGROUP --location "$RESOURCEGROUP_LOCATION"

Write-Host "Creating App Insights $AINAME and Log Analytics $LOGSNAME.."
az deployment group create -g $RESOURCEGROUP -f "$here/setup_template.bicep" `
                           -p workspace_name="$LOGSNAME" appinsights_name="$AINAME" location="$RESOURCEGROUP_LOCATION"

Write-Host "Creating non-container app components: Storage, ACR, KeyVault..."
az deployment group create -g $RESOURCEGROUP -f "$here/main.bicep" `
                           -p storageAccountName="$STORAGENAME" location="$RESOURCEGROUP_LOCATION" appEnvironmentName="$ENVNAME" keyVaultName="$VAULTNAME" storageAccountName="$STORAGENAME" acrName="$ACRNAME"

$STORAGE_CONNSTRING = $(az storage account show-connection-string -n $STORAGENAME --query connectionString -o tsv).Trim()
$ACRKEY = $(az acr credential show -n $ACRNAME --query passwords[0].value -o tsv).Trim()
$LOGKEY=$(az monitor log-analytics workspace get-shared-keys -g $RESOURCEGROUP --workspace-name $LOGSNAME --query 'primarySharedKey' -o tsv).Trim()
$LOGID=$(az monitor log-analytics workspace list -g $RESOURCEGROUP --query '[].customerId' -o tsv).Trim()
$APPINSIGHTSKEY=$(az resource show -g $RESOURCEGROUP -n $AINAME --resource-type "Microsoft.Insights/components" --query properties.InstrumentationKey -o tsv).Trim()


#
# Switch to custom cloud for container app deploy steps
#
az cloud set --name CloudWithCustomEndpoint
az login
az account set -s "Microsoft Azure Sponsorship"

Write-Host "Creating Worker Apps environment $ENVNAME.."
az containerapp env create -g "$RESOURCEGROUP" -n "$ENVNAME" --logs-workspace-id "$LOGID" --logs-workspace-key "$LOGKEY" --instrumentation-key "$APPINSIGHTSKEY" --location $LOCATION --no-wait
az containerapp env wait -g "$RESOURCEGROUP" -n "$ENVNAME" --created

# Prepare environment variables for passing into the container app
$envVars = @{
    TenantCloudBlobContainerFactoryOptions__AzureServicesAuthConnectionString = "x"
    RootTenantBlobStorageConfigurationOptions__AccountName = $STORAGE_CONNSTRING
}
$cliEnvs = $envVars.Keys | % { "{0}={1}" -f $_, $envVars[$_].Replace("=", "%3D") }

Write-Host "Deploying Tenancy container app $APP_NAME"
az containerapp create --name $APP_NAME `
                       --resource-group $RESOURCEGROUP `
                       --environment $ENVNAME `
                       --image $APP_IMAGE `
                       --target-port 80 `
                       --ingress 'external' `
                       --registry-login-server "$ACRNAME.azurecr.io" `
                       --registry-username $ACRNAME `
                       --registry-password $ACRKEY `
                       --environment-variables "$($cliEnvs -join ',')"

az containerapp show --resource-group $RESOURCEGROUP --name $APP_NAME --query "{FQDN:configuration.ingress.fqdn,ProvisioningState:provisioningState}" --out table



#
# Cleardown process
#

# az cloud set --name CloudWithCustomEndpoint
# az login
# az account set -s "Microsoft Azure Sponsorship"
# az containerapp delete --name $APP_NAME --resource-group $RESOURCEGROUP -y
# az containerapp env delete --name $ENVNAME --resource-group $RESOURCEGROUP -y

# az cloud set --name AzureCloud
# az login
# az account set -s "Microsoft Azure Sponsorship"
# az group delete -n $RESOURCEGROUP -y