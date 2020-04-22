# merge app-specific configuration with the shared repos
$deployConfig = $commonConfig, $environmentConfig | Merge-Hashtables `
    @{
        SingleServiceToDeploy = 'Marain.Tenancy'
        DeploymentAssetLocalOverrides = @{
            'Marain.Tenancy' = ('{0}/../Solutions/Marain.Tenancy.Deployment' -f $here)
        }
    }

$RequiredAppRoles = @(
    @{
        AppRoleId = "7619c293-764c-437b-9a8e-698a26250efd"
        DisplayName = "Tenancy administrator"
        Description = "Ability to create, modify, read, and remove tenants"
        Value = "TenancyAdministrator"
        AllowedMemberTypes = @("User", "Application")
    },
    @{
        AppRoleId = "60743a6a-63b6-42e5-a464-a08698a0e9ed"
        DisplayName = "Tenancy reader"
        Description = "Ability to read information about tenants"
        Value = "TenancyReader"
        AllowedMemberTypes = @("User", "Application")
    }
)