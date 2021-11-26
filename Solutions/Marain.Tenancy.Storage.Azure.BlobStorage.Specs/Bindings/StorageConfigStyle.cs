// <copyright file="StorageConfigStyle.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings
{
    internal enum StorageConfigStyle
    {
        /// <summary>
        /// Create the tenant using whichever style the root is configurated to propagate as.
        /// </summary>
        SameAsRootPropagationSetting,

        /// <summary>
        /// Create the tenant using v2 style configuration.
        /// </summary>
        V2,

        /// <summary>
        /// Create the tenant using v3 style configuration.
        /// </summary>
        V3,
    }
}
