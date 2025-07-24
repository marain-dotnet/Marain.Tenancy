namespace Marain.Tenancy.Client.Models;

/// <summary>
/// An operation describing one change to a tenant as part of an UpdateTenantJsonPatchArray
/// </summary>
public record UpdateTenantJsonPatchEntry(
	string Path,
	UpdateTenantJsonPatchEntryOperation Operation,
	object? Value)
{
}
