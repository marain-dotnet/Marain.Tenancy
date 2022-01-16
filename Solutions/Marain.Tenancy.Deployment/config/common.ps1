@{
    RequiredConfiguration = @(
        'AzureLocation'
        'ServiceName'
        'ApiAppName'
        'TenancyStorageSku'
    )

    TenancyStorageSecretName = 'TenancyStorageAccountKey'
    ServiceName = 'tenancy'
    ApiAppName = 'api'

    TenancySpCredentialSecretName = "TenancySpCredential"
    TenancyAdminSpCredentialSecretName = "TenancyAdminSpCredential"
}