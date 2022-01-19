#
# This sample represents the simplest Marain environment
#   - Dedicated Container App Environment, ACR & AppConfiguration
#   - Single key vault shared by all Marain services
#
@{
    AzureLocation = "northeurope"
    
    AadDeploymentManagedIdentityName = ""
    AadDeploymentManagedIdentityResourceGroupName = ""
    AadDeploymentManagedIdentitySubscriptionId = ""

    # All marain services will use the same key vault provisioned by the instace deployment
    UseSharedKeyVault = $true   # When true, all marain services will use a single key vault provisioned in the 'marain' resource group
                                # When false, each service will provision it's own key vault alongside its other resources
    # Only required, if you need to use a pre-existing custom shared key vault
    # SharedKeyVaultResourceGroupName = ""
    # SharedKeyVaultName = ""

    # use an existing app config store
    UseExistingAppConfigurationStore = $false       # When false, an AppConfig store will be provisioned in the 'marain' resource group
                                                    # When true, uncomment the settings below to configure the details
    # AppConfigurationStoreName = ""
    # AppConfigurationStoreResourceGroupName = ""
    # AppConfigurationStoreSubscriptionId = ""      # Only required if in a different subscription


    # Config for using an Azure container registry
    UseExistingAcr = $false     # When false, an ACR will be provisioned in the 'marain' resource group
                                # When true, uncomment the settings below to configure the details
    # AcrName = ""
    # AcrResourceGroupName = ""
    # AcrSubscriptionId = ""    # Only required if in a different subscription
 
    # Config for using a non-Azure container registry
    UseNonAzureContainerRegistry = $false   # When true, configure the settings below with the required details
                                            # When true, also implies 'UseExistingAcr=$true'
    ContainerRegistryServer = ""
    ContainerRegistryUser = ""
    ContainerRegistryKeySecretName = ""

    TenancyStorageSku = "Standard_LRS"
    
    # Name overrides (otherwise names will be generated based on stack, instance & environment details)
    TenancyResourceGroupName = ""
    TenancyStorageName = ""
    TenancyAppName = ""
    KeyVaultName = ""
}