// <copyright file="ApiResponseWithHeaders{T}.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Steps;

using Microsoft.Kiota.Abstractions;

public record ApiResponseWithHeaders<T>(T? Body, RequestHeaders Headers)
{
}