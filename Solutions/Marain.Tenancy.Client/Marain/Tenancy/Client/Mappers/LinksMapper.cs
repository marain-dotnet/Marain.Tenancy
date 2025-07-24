// <copyright file="ClientTenantProviderOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client.Mappers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Marain.Tenancy.Client.Internal.Models;
using Marain.Tenancy.Client.Models;
using Microsoft.Kiota.Abstractions.Serialization;

internal class LinksMapper
{
	internal static IDictionary<string, IImmutableList<Link>> FromInternalModel(Resource__links? source)
	{
		if (source is null)
		{
			return ImmutableDictionary<string, IImmutableList<Link>>.Empty;
		}

		ImmutableDictionary<string, IImmutableList<Link>>.Builder builder = ImmutableDictionary.CreateBuilder<string, IImmutableList<Link>>();

		foreach (KeyValuePair<string, object> item in source.AdditionalData)
		{
            if (TryMapObjectToResource(item.Value, out IImmutableList<Link>? mapped))
			{
				builder.Add(item.Key, mapped);
			}
		}

		return builder.ToImmutable();
	}

	private static bool TryMapObjectToResource(object source, [NotNullWhen(true)] out IImmutableList<Link>? result)
	{
		// When Kiota doesn't know what an object is, it maps it to one of its Untyped classes. In this case, we would expect an UntypedObject.
		if (source is UntypedObject untypedSource)
		{
            return TryMapUntypedObjectToResource(untypedSource, out result);
		}

		result = null;
		return false;
	}
	
	private static bool TryMapUntypedObjectToResource(UntypedObject source, [NotNullWhen(true)] out IImmutableList<Link>? result)
	{
		ImmutableList<Link>.Builder builder = ImmutableList.CreateBuilder<Link>();

		// Try and extract the necessary values from the object
		// TODO: Handle array scenario
		IDictionary<string, UntypedNode> properties = source.GetValue();

		string href = GetStringValue(properties, "href", string.Empty)!;
		bool templated = GetBoolValue(properties, "templated", false);
		string? type = GetStringValue(properties, "type");
		string? name = GetStringValue(properties, "name");
		string? profile = GetStringValue(properties, "profile");
		string? title = GetStringValue(properties, "title");
		string? hreflang = GetStringValue(properties, "hreflang");

        builder.Add(new Link(href, templated, type, name, profile, title, hreflang));

		result = builder.ToImmutable();
		return true;
	}

	private static string? GetStringValue(IDictionary<string, UntypedNode> properties, string name, string? defaultValue = null)
	{
		if (properties.TryGetValue(name, out UntypedNode? val))
		{
			if (val is UntypedString valAsString)
			{
				return valAsString.GetValue();
			}
		}

		return defaultValue;
	}

	private static bool GetBoolValue(IDictionary<string, UntypedNode> properties, string name, bool defaultValue = false)
	{
		if (properties.TryGetValue(name, out UntypedNode? val))
		{
			if (val is UntypedBoolean valAsBool)
			{
				return valAsBool.GetValue();
			}
		}

		return defaultValue;
	}
}
