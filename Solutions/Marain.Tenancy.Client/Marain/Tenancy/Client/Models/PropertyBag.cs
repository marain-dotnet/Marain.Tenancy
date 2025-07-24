using System.Collections.Generic;
using System.Collections.Immutable;

namespace Marain.Tenancy.Client.Models;

public record PropertyBag
{
    public PropertyBag(IDictionary<string, object>? additionalData)
    {
        this.AdditionalData = additionalData?.ToImmutableDictionary() ?? ImmutableDictionary<string, object>.Empty;
    }

    public IImmutableDictionary<string, object> AdditionalData { get; private set; }

    public static PropertyBag Empty { get; } = new((IDictionary<string, object>?)null);
}
