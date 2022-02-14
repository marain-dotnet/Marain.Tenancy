// <copyright file="TenancyApi.feature.multi.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Features
{
    using Marain.Tenancy.Specs.MultiHost;

    /// <summary>
    /// Add multi-host testing to SpecFlow-generated class.
    /// </summary>
    [MultiHostTest]

    public partial class TenancyApiFeature : MultiTestHostBase
    {
        public TenancyApiFeature(TestHostModes hostMode)
            : base(hostMode)
        {
        }
    }
}