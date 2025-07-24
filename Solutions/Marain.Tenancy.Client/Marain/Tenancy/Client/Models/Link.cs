namespace Marain.Tenancy.Client.Models;

/// <summary>
/// Represents a hyperlink from the containing resource to a URI.
/// </summary>
public record Link
{
	public Link(string href, bool templated = false, string? type = null, string? name = null, string? profile = null, string? title = null, string? hreflang = null)
	{
		this.Href = href;
		this.Templated = templated;
		this.Type = type;
		this.Name = name;
		this.Profile = profile;
		this.Title = title;
		this.Hreflang = hreflang;
	}

	/// <summary>
	/// URI of the target resource
	/// </summary>
	/// <remarks>
	/// Either a URI[RFC3986] or URI Template[RFC6570] of the target resource.
	/// </remarks>
	public string Href { get; }

	/// <summary>
	/// URI Template
	/// </summary>
	/// <remarks>
	/// Is true when the link object's href property is a URI Template. Defaults to false.
	/// </remarks>
	public bool Templated { get; }

	/// <summary>
	/// Media type indication of the target resource
	/// </summary>
	/// <remarks>
	/// When present, used as a hint to indicate the media type expected when dereferencing the target resource.
	/// </remarks>
	public string? Type { get; }

	/// <summary>
	/// Secondary key
	/// </summary>
	/// <remarks>
	/// When present, may be used as a secondary key for selecting link objects that contain the same relation type.
	/// </remarks>
	public string? Name { get; }

	/// <summary>
	/// Additional semantics of the target resource
	/// </summary>
	/// <remarks>
	/// A URI that, when dereferenced, results in a profile to allow clients to learn about additional semantics (constraints, conventions,
	/// extensions) that are associated with the target resource representation, in addition to those defined by the HAL media type and relations.
	/// </remarks>
	public string? Profile { get; }

	/// <summary>
	/// Human-readable identifier
	/// </summary>
	/// <remarks>
	/// When present, is used to label the destination of a link such that it can be used as a human-readable identifier(e.g.a menu entry)
	/// in the language indicated by the Content-Language header(if present).
	/// </remarks>
	public string? Title { get; }

	/// <summary>
	/// Language indication of the target resource[RFC5988]
	/// </summary>
	/// <remarks>
	/// When present, is a hint in RFC5646 format indicating what the language of the result of dereferencing the link should be.Note that 
	/// this is only a hint; for example, it does not override the Content-Language header of a HTTP response obtained by actually following 
	/// the link.
	/// </remarks>
	public string? Hreflang { get; }
}