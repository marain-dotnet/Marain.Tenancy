using System.Collections.Generic;
using System;
using System.Collections.Immutable;

namespace Marain.Tenancy.Client.Models;

public record Tenant : Resource
{
    public Tenant(
		string? id,
		string? name,
		string? contentType,
		string? etag,
		PropertyBag? properties,
		IDictionary<string, Resource> embeddedResources,
		IDictionary<string, IImmutableList<Link>> links
		) : base(embeddedResources, links)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id, nameof(id));
		ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
		ArgumentException.ThrowIfNullOrWhiteSpace(contentType, nameof(contentType));

		this.Id = id;
		this.Name = name;
		this.ContentType = contentType;
		this.ETag = etag;
		this.Properties = properties ?? PropertyBag.Empty;
    }

	public string ContentType { get; set; }


	public string? ETag { get; set; }

	/// <summary>The unique ID of the tenant. This forms a path with parent tenants.</summary>

	public string Id { get; set; }

	/// <summary>The name of the tenant.</summary>

	public string Name { get; set; }


	public PropertyBag Properties { get; set; }

}
