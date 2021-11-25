// <copyright file="EnumerateChildTenants.feature.multi.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Features
{
    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.MultiMode;

    /// <summary>
    /// Adds in multi-host-mode execution.
    /// </summary>
    [MultiSetupTest]
    public partial class EnumerateChildTenantsFeature : MultiSetupTestBase
    {
        /// <summary>
        /// Creates a <see cref="EnumerateChildTenantsFeature"/>.
        /// </summary>
        /// <param name="hostMode">
        /// Hosting style to test for.
        /// </param>
        public EnumerateChildTenantsFeature(SetupModes hostMode)
            : base(hostMode)
        {
        }
    }
}