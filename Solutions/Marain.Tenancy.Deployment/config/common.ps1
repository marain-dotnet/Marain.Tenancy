@{
    RequiredConfiguration = @(
        'AzureLocation'
        'ServiceName'
        'ApiAppName'
        'TenancyStorageSku'
        'SharedKeyVaultName'
    )

    TenancyStorageSecretName = 'TenancyStorageAccountKey'
    ServiceName = 'tenancy'
    ApiAppName = 'api'
}