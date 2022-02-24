@{
    RequiredConfiguration = @(
        'AzureLocation'
        'ServiceName'
        'HostingEnvironmentType'
        # 'KeyVaultReadersGroupObjectId'
        # 'KeyVaultContributorsGroupObjectId'
    )

    ServiceName = 'tenancy'
    TenancyStorageSecretName = "TenancyStorageAccessKey"
    TenancyAdminSpCredentialSecretName = "TenancyAdminSpCredential"
}