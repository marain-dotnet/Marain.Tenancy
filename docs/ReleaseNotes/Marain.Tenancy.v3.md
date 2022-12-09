# Release notes for Marain.Tenancy v3.

## v3.1

* `Corvus.Identity.MicrosoftRest` 3.1 -> 3.2
    * `Marain.Tenancy.Client`
* `Corvus.Tenancy.Abstractions` 3.4 -> 3.5
    * `Marain.Tenancy.ClientTenantProvider`
    * `Marain.Tenancy.OpenApi.Service`
* `Corvus.Storage.Azure.BlobStorage.Tenancy` 3.4 -> 3.5
    * `Marain.Tenantc.Storage.Azure.BlobStorage`


## v3.0

Upgrades Menes from v3.0 to v4.0.

There are no significant code changes in this release. The major version bump is required only because Menes has also changed its major version number, so by semantic versioning rules, we also need to bump ours.

Client code should continue to work unaltered against tenancy services using this new version. The changes are all server side, so client applications will see no changes if they upgrade to the latest client library versions.