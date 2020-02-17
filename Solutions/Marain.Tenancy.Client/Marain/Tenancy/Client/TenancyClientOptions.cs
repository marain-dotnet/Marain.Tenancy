// <copyright file="ClientTenantProviderOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client
{
    using System;

    /// <summary>
    /// Settings for configuring the <see cref="TenancyService"/>.
    /// </summary>
    public class TenancyClientOptions
    {
        /// <summary>
        /// Gets or sets the base URL of the tenancy service.
        /// </summary>
        public Uri TenancyServiceBaseUri { get; set; }

        /// <summary>
        /// Gets or sets the resource ID to use when asking the Managed Identity system for a token
        /// with which to communicate with the tenancy service. This is typically the App ID of the
        /// application created for securing access to the tenancy service.
        /// </summary>
        /// <remarks>
        /// If this is null, no attempt will be made to secure communication with the tenancy
        /// service. This may be appropriate for local development scenarios.
        /// </remarks>
        public string ResourceIdForMsiAuthentication { get; set; }
    }
}
