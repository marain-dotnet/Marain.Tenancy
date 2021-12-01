// <copyright file="TenancyBlobStorageServiceCollectionExtensions.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Microsoft.Extensions.DependencyInjection
{
    using Corvus.Storage.Azure.BlobStorage;
    using Corvus.Tenancy;
    using Corvus.Tenancy.Internal;

    using Marain.Tenancy.Storage.Azure.BlobStorage;

    /// <summary>
    /// Configuration of services for using the operations repository implemented on top of the
    /// tenancy repository.
    /// </summary>
    public static class TenancyBlobStorageServiceCollectionExtensions
    {
        /// <summary>
        /// Enable tenancy storage over Azure Blob Storage.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="rootStorageConfiguration">
        /// Storage configuration to use when accessing the root tenant.
        /// </param>
        /// <param name="propagateRootTenancyStorageConfigAsV2">
        /// If true, when a child tenant is created, the tenancy storage settings copied from the
        /// parent tenant will be written in a V2 format regardless of whether the parent tenant is
        /// using V2 or V3.
        /// </param>
        /// <returns>The modified service collection.</returns>
        /// <remarks>
        /// <para>
        /// The <paramref name="propagateRootTenancyStorageConfigAsV2"/> setting enables a gradual
        /// transition of a system that had previously been using the Corvus.Tenancy V2
        /// <see cref="ITenantStore"/> over Azure Blob implementation. As the Corvus.Tenancy
        /// documentation describes, the first step of a transition from V2 to V3 entails upgrading
        /// all compute nodes with software that can understand V3 configuration, but which does
        /// not attempt to use it. This is necessary because it can take a while for updates to be
        /// rolled out across all nodes. If there is to be no downtime, there will be a time period
        /// in which some of the compute nodes have been updated but some are still running code
        /// that doesn't know how to use V3 configuration.
        /// </para>
        /// <para>
        /// This tenant store implementation is V3-capable. It supports transition, in that it can
        /// handle V2 configuration entries. However, it requires that the root configuration be
        /// supplied in V3 format. This creates a potential problem if anything creates new child
        /// tenants underneath the root. (Currently we don't expect this to happen often, since
        /// usually, there are just two tenants here: the parent of all service tenants and the
        /// parent of all client tenants. However, that's just a convention currently employed by
        /// code using tenant. Also, integration tests for this code need to be able to create
        /// tenants directly underneath the root.) If new children of the root just propagated
        /// configuration directly, the V3 configuration supplied here would end up everywhere.
        /// But we need that propagation to be able to downgrade the configuration to a V2
        /// format in cases where we're still in that initial phase.
        /// </para>
        /// <para>
        /// The <paramref name="propagateRootTenancyStorageConfigAsV2"/> argument enables this
        /// behaviour. It affects only the creation of children of the root. In all other cases,
        /// configuration is propagated without alteration.
        /// </para>
        /// </remarks>
        public static IServiceCollection AddTenantStoreOnAzureBlobStorage(
            this IServiceCollection services,
            BlobContainerConfiguration rootStorageConfiguration,
            bool propagateRootTenancyStorageConfigAsV2 = false)
        {
            services.AddAzureBlobStorageClient();

            // TODO: this next one is temporary.
            // Corvus.Storage.Azure.BlobStorage.Tenancy should offer some successor to
            // AddTenantCloudBlobContainerFactory, but since it currently only offers the
            // transitional API (there is not yet an implementation of a native-v3 API)
            // that hasn't been written yet, and so we need to call this bootstrapper
            // in the Corvus.Tenancy.Internal namespace.
            services.AddRequiredTenancyServices();

            // TODO: This probably should also be called by Corvus.Storage.Azure.BlobStorage.Tenancy
            services.AddAzureBlobStorageClient();

            services.AddBlobContainerV2ToV3Transition();
            services.AddSingleton(new AzureBlobStorageTenantStoreConfiguration(
                rootStorageConfiguration,
                propagateRootTenancyStorageConfigAsV2));
            services.AddSingleton<ITenantStore, AzureBlobStorageTenantStore>();
            return services;
        }
    }
}