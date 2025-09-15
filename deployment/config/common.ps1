@{
    RequiredConfiguration = @(
        'azureTenantId'
        'azureLocation'
        'azureSubscriptionId'
        'acrName'
        'acrResourceGroupName'
        'appInsightsWorkspaceName'
        'hostingEnvironmentName'
        'logAnalyticsWorkspaceName'
        'tenancyKeyVaultName'
        # 'keyVaultSecretsOfficerPrincipalId'
        'tenancyResourceGroupName'
        'tenancyServiceContainerImageName'
        'tenancyServiceManagedIdentityName'
        'tenancyServiceName'
        'tenancyStorageAccountName'
    )

    acrName = 'endjin'
    acrResourceGroupName = 'endjin-container-registry-prod-rg'
    acrSubscriptionId = '9a1d877d-6acd-40d3-92a1-ee057e8dcda4'
    enableAvmTelemetry = $false
    tenancyServiceContainerImageName = 'marain/tenancy-service'

    # tenancyServiceContainerImageTag = ''
}