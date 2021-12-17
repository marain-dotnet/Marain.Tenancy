# Release notes for Marain.Tenancy v3.

## v2.0

Targets .NET 6.0 only.
The function host runs on Functions v4.
This has been updated to use the Corvus.Tenancy v3, Corvus.Storage v1, and Corvus.Identity v3. (This in turn means we're now using the new-style (Azure.Core) Azure Client SDK—no more dependencies on deprecated client libraries.)

There are no changes at the web API level, so existing client code should continue to work unaltered against tenancy services using this new version.