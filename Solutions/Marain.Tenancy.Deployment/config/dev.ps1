@{
    AzureLocation = "northeurope"
    
    # All marain services will use the same key vault provisioned by the instace deployment
    UseSharedKeyVault = $true
    SharedKeyVaultResourceGroupName = "marainrgrpygiadtligd"
    SharedKeyVaultName = "marainkvrpygiadtligd"

    # use an existing app config store
    UseExistingAppConfigurationStore = $true
    AppConfigurationStoreResourceGroupName = "jd-config-test-rg"
    AppConfigurationStoreName = "JD-CONFIG-TEST"

    # use an existing container registry
    UseExistingAcr = $true
    AcrName = "endmarainspikeacr"
    AcrResourceGroupName = "rg-marain-workerapps-spike"
    AcrSubscriptionId = "0d19aa73-7029-4296-8852-d401a4d29cad"
    # ContainerRegistryServer = "endmarainspikeacr.azurecr.io"
    # ContainerRegistryUser = ""
    # ContainerRegistryKeySecretName = ""

    TenancyStorageSku = "Standard_LRS"
    TenancySpCredentialSecretName = "TenancySpCredential"
    
    # Name overrides (if set)
    TenancyResourceGroupName = ""
    TenancyStorageName = ""
    TenancyAppName = ""
    KeyVaultName = ""
}