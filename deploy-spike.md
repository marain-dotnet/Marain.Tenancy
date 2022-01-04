# Marain Deployment vNext Spike

This branch contains PoC deployment work that builds on some earlier work, including:
* https://github.com/marain-dotnet/Marain.Tenancy/tree/feature/corvus-deployment
* https://github.com/marain-dotnet/Marain.Tenancy/tree/feature/dapr
* https://github.com/corvus-dotnet/Corvus.Deployment/tree/feature/uniqueSuffix


## Overview

* Targets hosting in Azure Container Apps rather than Azure Functions
* ARM deployment migrated to Bicep:
    * [`Marain.Instance`](Solutions/instance-main.bicep) implemented in [deploy-instance.ps1](Solutions/deploy-instance.ps1)
        * Includes: Container App Environment, optional ACR, shared key vault & app config store, published config/secrets
    * [`Marain.Tenancy`](Solutions/tenancy-main.bicep) implemented in [deploy-tenancy.ps1](Solutions/deploy-tenancy.ps1)
        * Includes: Storage, container app, published config/secrets
    * Initial set of reusable Bicep modules
    * Intended to easily support use of existing Azure services (cross-subscription) or provisioning dedicated instances:
        * Container App Environment
        * Azure Container Registry
        * App Configuration Store
    * Support for using a single Marain key vault or the current vault per service approach
    * Aims to minimise the need to pass secrets in or out of the ARM deployment 
* Uses `Corvus.Deployment` and `InvokeBuild` as basis for the deployment process
* Opinonated config management approach as per Corvus.Deployment:
    * [common.ps1](Solutions/Marain.Tenancy.Deployment/config/dev.ps1) - shared settings across all environments
    * [dev.ps1](Solutions/Marain.Tenancy.Deployment/config/dev.ps1) - a sample dev environment using a MPN subscription
    * ARM deployment publishes required config to Key Vault and App Configuration


## Build

An initial [build](build.ps1) (using Endjin.RecommendedPractises.Build) has been setup and extended to produce a suitable container image.


## Endjin.RecommendedPractises

The [`erp`](erp/) folder is a placeholder for elements that, if retained, could be extracted into separate `Endjin.RecommendedPractises.*` repositories:

* `[erp/bicep]`(erp/bicep) - reusable Bicep modules
* `[erp/deploy]`(erp/deploy) - reusable deployment processes, equivalent to [Endjin.RecommendedPractises.Build](https://github.com/endjin/Endjin.RecommendedPractises.Build) but for deployment


## Testing the spike

The deployment of the Marain Instance and Marain Tenancy infrastructure & services can be run as follows:

```
az login
az account set -s <subscription-id>
Connect-AzAccount
Set-AzContext -SubscriptionId <subscription-id>
./Solutions/deploy-runner.ps1 -Environment sample -StackName <your-initials> -ServiceInstance i1
```

You can also experiment with different configurations by modifying a copy of the [`sample.ps1`](Solutions\Marain.Tenancy.Deployment\config\sample.ps1) configuration script.
