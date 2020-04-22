// <copyright file="TenantMapper.cs" company="Endjin Limited">
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
    public class TenantMapper : IHalDocumentMapper<ITenant>
    {
        private readonly IHalDocumentFactory halDocumentFactory;
        private readonly IOpenApiWebLinkResolver linkResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantMapper"/> class.
        /// </summary>
        /// <param name="halDocumentFactory">The service provider to construct <see cref="HalDocument"/> instances.</param>
        /// <param name="linkResolver">The link resolver to build the links collection.</param>
        public TenantMapper(IHalDocumentFactory halDocumentFactory, IOpenApiWebLinkResolver linkResolver)
        {
            this.halDocumentFactory = halDocumentFactory;
            this.linkResolver = linkResolver;
        }

        /// <inheritdoc/>
        public void ConfigureLinkMap(IOpenApiLinkOperationMap links)
        {
            links.MapByContentTypeAndRelationTypeAndOperationId(Tenant.RegisteredContentType, "self", TenancyService.GetTenantOperationId);
            links.MapByContentTypeAndRelationTypeAndOperationId(Tenant.RegisteredContentType, "children", TenancyService.GetChildrenOperationId);
        }

        /// <inheritdoc/>
        public HalDocument Map(ITenant input)
        {
            HalDocument response = this.halDocumentFactory.CreateHalDocumentFrom(input);
            response.ResolveAndAddByOwnerAndRelationType(this.linkResolver, input, "self", ("tenantId", input.Id));
            response.ResolveAndAddByOwnerAndRelationType(this.linkResolver, input, "children", ("tenantId", input.Id));

            return response;
        }
    }
}
