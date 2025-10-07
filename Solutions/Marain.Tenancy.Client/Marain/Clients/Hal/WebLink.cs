// <copyright file="WebLink.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Clients.Hal;

/// <summary>
/// Represents a hypermedia link to a related resource. Follows the general pattern described
/// in RFC 8288 (https://tools.ietf.org/html/rfc8288).
/// </summary>
public record WebLink
{
    /// <summary>
    /// Gets the URI of the target resource.
    /// </summary>
    /// <remarks>
    /// Either a URI [RFC3986] or URI Template [RFC6570] of the target
    /// resource.
    /// </remarks>
    public required string Href { get; init; }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <remarks>
    /// When present, may be used as a secondary key for selecting link
    /// objects that contain the same relation type.
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>
    /// Gets a value that indicates whether the link is templated (or null if the property is not set).
    /// </summary>
    /// <remarks>
    /// Is true when the link object's href property is a URI Template.
    /// Defaults to false.
    /// </remarks>
    public bool? IsTemplated { get; init; }

    /// <summary>
    /// Gets a value for the human-readable title of the link.
    /// </summary>
    /// <remarks>
    /// When present, is used to label the destination of a link such that
    /// it can be used as a human-readable identifier (e.g. a menu entry)
    /// in the language indicated by the Content-Language header (if
    /// present).
    /// </remarks>
    public string? Title { get; init; }

    /// <summary>
    /// Gets additional semantics of the target resource.
    /// </summary>
    /// <remarks>
    /// A URI that, when dereferenced, results in a profile to allow
    /// clients to learn about additional semantics (constraints,
    /// conventions, extensions) that are associated with the target
    /// resource representation, in addition to those defined by the HAL
    /// media type and relations.
    /// </remarks>
    public string? Profile { get; init; }

    /// <summary>
    /// Gets media type indication of the target resource.
    /// </summary>
    /// <remarks>
    /// When present, used as a hint to indicate the media type expected
    /// when dereferencing the target resource.
    /// </remarks>
    public string? Type { get; init; }

    /// <summary>
    /// Gets the language indication of the target resource [RFC5988].
    /// </summary>
    /// <remarks>
    /// When present, is a hint in RFC5646 format indicating what the
    /// language of the result of dereferencing the link should be.  Note
    /// that this is only a hint; for example, it does not override the
    /// Content-Language header of a HTTP response obtained by actually
    /// following the link.
    /// </remarks>
    public string? Hreflang { get; init; }
}