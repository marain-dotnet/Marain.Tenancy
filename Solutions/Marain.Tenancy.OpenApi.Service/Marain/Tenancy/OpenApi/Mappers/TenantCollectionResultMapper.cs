﻿// <copyright file="TenantCollectionResultMapper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.OpenApi.Mappers
{
    using Corvus.Tenancy;
    using Menes;
    using Menes.Hal;
    using Menes.Links;

    /// <summary>
    /// Maps Tenants to tenant resources.
    /// </summary>
    public class TenantCollectionResultMapper : IHalDocumentMapper<TenantCollectionResult>
    {
        private readonly IHalDocumentFactory halDocumentFactory;
        private readonly IOpenApiWebLinkResolver linkResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantCollectionResultMapper"/> class.
        /// </summary>
        /// <param name="halDocumentFactory">The service provider to construct <see cref="HalDocument"/> instances.</param>
        /// <param name="linkResolver">The link resolver to build the links collection.</param>
        public TenantCollectionResultMapper(IHalDocumentFactory halDocumentFactory, IOpenApiWebLinkResolver linkResolver)
        {
            this.halDocumentFactory = halDocumentFactory;
            this.linkResolver = linkResolver;
        }

        /// <inheritdoc/>
        public void ConfigureLinkMap(IOpenApiLinkOperationMap links)
        {
        }

        /// <inheritdoc/>
        public HalDocument Map(TenantCollectionResult input)
        {
            HalDocument response = this.halDocumentFactory.CreateHalDocument();
            foreach (string tenantId in input.Tenants)
            {
                response.AddLink("tenants", this.linkResolver.Resolve(TenancyService.GetTenantOperationId, "self", ("tenantId", tenantId)));
            }

            return response;
        }
    }
}