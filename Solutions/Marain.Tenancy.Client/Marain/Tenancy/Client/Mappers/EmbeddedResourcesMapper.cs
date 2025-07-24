namespace Marain.Tenancy.Client.Mappers;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Marain.Tenancy.Client.Models;
using InternalModels = Internal.Models;

internal static class EmbeddedResourcesMapper
{
	internal static IDictionary<string, Resource> FromInternalModel(InternalModels.Resource__embedded? source)
	{
		if (source is null)
		{
			return ImmutableDictionary<string, Resource>.Empty;
		}

        ImmutableDictionary<string, Resource>.Builder builder = ImmutableDictionary.CreateBuilder<string, Resource>();

        foreach (KeyValuePair<string, object> item in source.AdditionalData)
		{
            if (TryMapObjectToResource(item.Value, out Resource? result))
			{
				builder.Add(item.Key, result);
			}
		}

		return builder.ToImmutable();
	}

	private static bool TryMapObjectToResource(object source, [NotNullWhen(true)] out Resource? resource)
	{
		// TODO: how to map this?
		resource = null;
		return false;
	}
}
