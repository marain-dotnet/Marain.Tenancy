// <copyright file="GetTenant.feature.multi.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Features
{
    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.MultiMode;

    /// <summary>
    /// Adds in multi-host-mode execution.
    /// </summary>
    [MultiSetupTest]
    public partial class GetTenantFeature : MultiSetupTestBase
    {
        /// <summary>
        /// Creates a <see cref="GetTenantFeature"/>.
        /// </summary>
        /// <param name="hostMode">
        /// Hosting style to test for.
        /// </param>
        public GetTenantFeature(SetupModes hostMode)
            : base(hostMode)
        {
        }
    }
}