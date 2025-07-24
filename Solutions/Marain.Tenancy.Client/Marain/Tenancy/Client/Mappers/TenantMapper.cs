namespace Marain.Tenancy.Client.Mappers;

using Marain.Tenancy.Client.Models;
using InternalModels = Marain.Tenancy.Client.Internal.Models;

internal static class TenantMapper
{
	internal static Tenant FromInternalModel(InternalModels.Tenant source)
	{
		return new(
			source.TenantMember1?.Id,
			source.TenantMember1?.Name,
			source.TenantMember1?.ContentType,
			source.TenantMember1?.ETag,
			new(source.TenantMember1?.Properties?.AdditionalData),
			EmbeddedResourcesMapper.FromInternalModel(source.Resource?.Embedded),
			LinksMapper.FromInternalModel(source.Resource?.Links));
	}
}
