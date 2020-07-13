// <copyright file="TenantCollectionResultMapper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.OpenApi.Mappers
{
    using System.Threading.Tasks;
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
            links.MapByContentTypeAndRelationTypeAndOperationId(Tenant.RegisteredContentType, "delete", TenancyService.DeleteChildTenantOperationId);
        }

        /// <inheritdoc/>
        public ValueTask<HalDocument> MapAsync(TenantCollectionResult input)
        {
            HalDocument response = this.halDocumentFactory.CreateHalDocument();
            foreach (string tenantId in input.Tenants)
            {
                string? parentId = tenantId.GetParentId();

                response.AddLink(
                    "getTenant",
                    this.linkResolver.ResolveByOperationIdAndRelationType(TenancyService.GetTenantOperationId, "self", ("tenantId", tenantId)));

                if (parentId != null)
                {
                    response.AddLink(
                        "deleteTenant",
                        this.linkResolver.ResolveByOperationIdAndRelationType(TenancyService.DeleteChildTenantOperationId, "delete", ("tenantId", parentId), ("childTenantId", tenantId)));
                }
            }

            return new ValueTask<HalDocument>(response);
        }
    }
}