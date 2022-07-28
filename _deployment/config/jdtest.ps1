@{
    # Settings potentially common to all Marain components
    azureLocation = "northeurope"

    aadDeploymentManagedIdentityName = "ms-graph-deployment-mi"
    aadDeploymentManagedIdentityResourceGroupName = "deployment-managed-identities"
    aadDeploymentManagedIdentitySubscriptionId = "9a1d877d-6acd-40d3-92a1-ee057e8dcda4"

    hostingPlatformType = 'Functions'
    
    appInsightsInstrumentationKeySecretName = "AppInsightsInstrumentationKey"

    appConfigurationStoreName = "marain-config-jdtest"
    appConfigurationStoreResourceGroupName = "marain-instance-jdtest-rg"
    appConfigurationLabel = "jdtest"

    keyVaultSecretsReadersGroupObjectId = "8e25cffb-9150-473e-8917-8f10e432e4ed"
    keyVaultSecretsContributorsGroupObjectId = "78f0285a-0004-4b92-b2f8-e1c2cbcbe4f2"

    resourceTags = @{ environment = "jdtest" }

    # Tenancy-specific config
    tenancyResourceGroupName = "marain-tenancy-jdtest-rg"
    tenancystorageAccountName = "maraintenancyjdtestsa"
    tenancystorageSku = "Standard_LRS"
    tenancyAppName = "marain-tenancy-api-jdtest"
    tenancyAppVersion = "1.1.14"
    tenancyFunctionStorageAccountName = "maraintenancyjdtestfunc"
    tenancyStorageSecretName = "TenancyStorageAccessKey"

    useExistingKeyVault = $true
    tenancyKeyVaultName = "marainisntancejdtestkv"
    tenancyKeyVaultResourceGroupName = "marain-instance-jdtest-rg"

    tenancyHostingPlatformResourceGroupName = ""
    tenancyHostingPlatformName = "marain-tenancy-hosting-jdtest"
    tenancyUseExistingHosting = $false

    tenancyAdminSpName = "marain-tenancy-admin-jdtest-sp"
    tenancyAdminSpCredentialSecretName = "TenancyAdminSpCredential"
    tenancyAppRegistrationName = "marain-tenancy-jdtest"
}