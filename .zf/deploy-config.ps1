# Extensions setup
$zerofailedExtensions = @(
    @{
        Name = "ZeroFailed.Deploy.Azure"
        GitRepository = "https://github.com/zerofailed/ZeroFailed.Deploy.Azure.git"
        GitRef = "main"
    }
)

# Load the tasks and process
. ZeroFailed.tasks -ZfPath $here/.zf

Set-StrictMode -Version 4

#
# ARM deployment configuration
#
$RequiredArmDeployments = @(
    @{
        templatePath       = "$here/deployment/bicep/main.bicep"
        resourceGroupName  = { $deploymentConfig.tenancyResourceGroupName }
        location           = { $deploymentConfig.azureLocation }
        # ZeroFailed uses a convention whereby configuration settings are assumed to match ARM deployment parameters.
        # This value overrides this behaviour by removing any config settings not required for ARM deployment,
        # or with an empty value so the ARM parameter defaults can be used.
        configKeysToIgnore = @(
            "RequiredConfiguration"
            "azureLocation"
            "azureSubscriptionId"
            "azureTenantId"
            "tenancyResourceGroupName"
        )
    }
)


# Synopsis: Use the standard ZeroFailed deployment process
task . FullDeployment

