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

    # Expected to be provided by the deployment server, to avoid storing the details in this OSS git repo
    azureSubscriptionId = '@EnvironmentVariable(AZURE_SUBSCRIPTION_ID)'
    azureTenantId = '@EnvironmentVariable(AZURE_TENANT_ID)'
    acrName = '@EnvironmentVariable(BUILD_CONTAINER_REGISTRY_FQDN)'
    acrResourceGroupName = '@EnvironmentVariable(BUILD_ACR_RESOURCE_GROUP_NAME)'
    acrSubscriptionId = '@EnvironmentVariable(BUILD_ACR_SUBSCRIPTION_ID)'

    enableAvmTelemetry = $false
    tenancyServiceContainerImageName = 'marain/tenancy-service'

    # tenancyServiceContainerImageTag = ''
}