// <copyright file="SetupModes.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.MultiMode
{
    /// <summary>
    /// Determines how tests set up pre-existing tenants.
    /// </summary>
    /// <remarks>
    /// This enables us to run tests in two different ways: 1) verifying that the code works
    /// against stores originally populated with an earlier version; 2) verifying that the
    /// code works entirely in its own right.
    /// </remarks>
    public enum SetupModes
    {
        /// <summary>
        /// All setup will be done through the code under test. The store is configured to write
        /// V2-style config when propagating settings from the root.
        /// </summary>
        ViaApiPropagateRootConfigAsV2,

        /// <summary>
        /// All setup will be done through the code under test. The store is configured to write
        /// V3-style config when propagating settings from the root.
        /// </summary>
        ViaApiPropagateRootConfigAsV3,

        /// <summary>
        /// Setup will be done by writing data directly into Azure Storage. The store is configured
        /// to write V3-style config when propagating settings from the root.
        /// </summary>
        DirectToStoragePropagateRootConfigAsV2,

        /// <summary>
        /// Setup will be done by writing data directly into Azure Storage. The store is configured
        /// to write V3-style config when propagating settings from the root.
        /// </summary>
        DirectToStoragePropagateRootConfigAsV3,
    }
}