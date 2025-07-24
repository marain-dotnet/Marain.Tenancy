namespace Marain.Tenancy.Client.Models;

using System.Collections.Immutable;

public record GetChildrenResult(
	IImmutableList<Link> ChildTenantGetLinks,
	IImmutableList<Link> ChildTenantDeleteLinks,
	Link NextPageLink)
{
}
