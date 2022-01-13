@{
    AzureLocation = "northeurope"
    
    AadDeploymentManagedIdentityName = 'graph-api-deploy-identity'
    AadDeploymentManagedIdentityResourceGroupName = 'jd-config-test-rg'
    AadDeploymentManagedIdentitySubscriptionId = '13821a69-41a3-43ab-9577-1519963ea474'
    
    # All marain services will use the same key vault provisioned by the instance deployment
    UseSharedKeyVault = $true
    # SharedKeyVaultResourceGroupName = ""
    # SharedKeyVaultName = ""

    # use an existing app config store
    UseExistingAppConfigurationStore = $true
    AppConfigurationStoreResourceGroupName = "jd-config-test-rg"
    AppConfigurationStoreName = "JD-CONFIG-TEST"

    # use an existing container registry
    UseExistingAcr = $true
    AcrName = "endmarainspikeacr"
    AcrResourceGroupName = "rg-marain-workerapps-spike"
    AcrSubscriptionId = "0d19aa73-7029-4296-8852-d401a4d29cad"
    UseNonAzureContainerRegistry = $false
    # The below are unused, but are currently required to be blank
    ContainerRegistryServer = ""
    ContainerRegistryUser = ""
    ContainerRegistryKeySecretName = ""

    TenancyStorageSku = "Standard_LRS"
    
    # Name overrides (if set)
    TenancyResourceGroupName = ""
    TenancyStorageName = ""
    TenancyAppName = ""
    KeyVaultName = ""
}