// <copyright file="SerializationBindings.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Bindings;

using Corvus.Testing.ReqnRoll;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

[Binding]
public static class SerializationBindings
{
    [BeforeFeature(Order = ContainerBeforeFeatureOrder.PopulateServiceCollection)]
    public static void SetupSerialization(FeatureContext featureContext)
    {
        ContainerBindings.ConfigureServices(
            featureContext,
            serviceCollection =>
            {
                serviceCollection.AddJsonSerializerOptionsProvider();
                serviceCollection.AddJsonCultureInfoConverter();
                serviceCollection.AddJsonDateTimeOffsetToIso8601AndUnixTimeConverter();
                serviceCollection.AddCamelCaseConverterForEnums();
                serviceCollection.AddJsonPropertyBagFactory();
            });
    }
}