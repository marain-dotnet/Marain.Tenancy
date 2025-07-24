namespace Marain.Tenancy.Client.Mappers;

using System;
using Marain.Tenancy.Client.Models;
using InternalModels = Internal.Models;

internal static class GetChildrenResultMapper
{
	internal static GetChildrenResult FromInternalModel(InternalModels.Resource resource)
	{
		// The children will be contained in the embedded documents.

	}
}
