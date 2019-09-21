// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Marain.Tenancy.Client
{
    using Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for TenancyService.
    /// </summary>
    public static partial class TenancyServiceExtensions
    {
            /// <summary>
            /// Update a tenant
            /// </summary>
            /// <remarks>
            /// Updates the tenant
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tenantId'>
            /// The tenant within which the request should operate
            /// </param>
            /// <param name='body'>
            /// </param>
            public static Tenant UpdateTenant(this ITenancyService operations, string tenantId, Tenant body)
            {
                return operations.UpdateTenantAsync(tenantId, body).GetAwaiter().GetResult();
            }

            /// <summary>
            /// Update a tenant
            /// </summary>
            /// <remarks>
            /// Updates the tenant
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tenantId'>
            /// The tenant within which the request should operate
            /// </param>
            /// <param name='body'>
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<Tenant> UpdateTenantAsync(this ITenancyService operations, string tenantId, Tenant body, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.UpdateTenantWithHttpMessagesAsync(tenantId, body, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <summary>
            /// Update a tenant
            /// </summary>
            /// <remarks>
            /// Gets the tenant
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tenantId'>
            /// The tenant within which the request should operate
            /// </param>
            public static Tenant GetTenant(this ITenancyService operations, string tenantId)
            {
                return operations.GetTenantAsync(tenantId).GetAwaiter().GetResult();
            }

            /// <summary>
            /// Update a tenant
            /// </summary>
            /// <remarks>
            /// Gets the tenant
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tenantId'>
            /// The tenant within which the request should operate
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<Tenant> GetTenantAsync(this ITenancyService operations, string tenantId, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.GetTenantWithHttpMessagesAsync(tenantId, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <summary>
            /// Get all child tenants of the current tenant
            /// </summary>
            /// <remarks>
            /// Get all child tenants of the current tenant
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tenantId'>
            /// The tenant within which the request should operate
            /// </param>
            /// <param name='continuationToken'>
            /// A continuation token for an operation where more data is available
            /// </param>
            public static ChildTenants GetChildren(this ITenancyService operations, string tenantId, string continuationToken = default(string))
            {
                return operations.GetChildrenAsync(tenantId, continuationToken).GetAwaiter().GetResult();
            }

            /// <summary>
            /// Get all child tenants of the current tenant
            /// </summary>
            /// <remarks>
            /// Get all child tenants of the current tenant
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tenantId'>
            /// The tenant within which the request should operate
            /// </param>
            /// <param name='continuationToken'>
            /// A continuation token for an operation where more data is available
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<ChildTenants> GetChildrenAsync(this ITenancyService operations, string tenantId, string continuationToken = default(string), CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.GetChildrenWithHttpMessagesAsync(tenantId, continuationToken, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <summary>
            /// Create a child tenant
            /// </summary>
            /// <remarks>
            /// Creates a child tenant of the parent tenant
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tenantId'>
            /// The tenant within which the request should operate
            /// </param>
            /// <param name='childTenantId'>
            /// The child tenant within the current tenant.
            /// </param>
            public static void CreateChildTenant(this ITenancyService operations, string tenantId, string childTenantId)
            {
                operations.CreateChildTenantAsync(tenantId, childTenantId).GetAwaiter().GetResult();
            }

            /// <summary>
            /// Create a child tenant
            /// </summary>
            /// <remarks>
            /// Creates a child tenant of the parent tenant
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tenantId'>
            /// The tenant within which the request should operate
            /// </param>
            /// <param name='childTenantId'>
            /// The child tenant within the current tenant.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task CreateChildTenantAsync(this ITenancyService operations, string tenantId, string childTenantId, CancellationToken cancellationToken = default(CancellationToken))
            {
                (await operations.CreateChildTenantWithHttpMessagesAsync(tenantId, childTenantId, null, cancellationToken).ConfigureAwait(false)).Dispose();
            }

            /// <summary>
            /// Delete a child tenant by ID
            /// </summary>
            /// <remarks>
            /// Deletes a child tenant of the parent tenant by ID
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tenantId'>
            /// The tenant within which the request should operate
            /// </param>
            /// <param name='childTenantId'>
            /// The child tenant within the current tenant.
            /// </param>
            public static Tenant DeleteChildTenant(this ITenancyService operations, string tenantId, string childTenantId)
            {
                return operations.DeleteChildTenantAsync(tenantId, childTenantId).GetAwaiter().GetResult();
            }

            /// <summary>
            /// Delete a child tenant by ID
            /// </summary>
            /// <remarks>
            /// Deletes a child tenant of the parent tenant by ID
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='tenantId'>
            /// The tenant within which the request should operate
            /// </param>
            /// <param name='childTenantId'>
            /// The child tenant within the current tenant.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<Tenant> DeleteChildTenantAsync(this ITenancyService operations, string tenantId, string childTenantId, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.DeleteChildTenantWithHttpMessagesAsync(tenantId, childTenantId, null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

            /// <summary>
            /// View swagger definition for this API
            /// </summary>
            /// <remarks>
            /// View swagger definition for this API
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            public static object GetSwagger(this ITenancyService operations)
            {
                return operations.GetSwaggerAsync().GetAwaiter().GetResult();
            }

            /// <summary>
            /// View swagger definition for this API
            /// </summary>
            /// <remarks>
            /// View swagger definition for this API
            /// </remarks>
            /// <param name='operations'>
            /// The operations group for this extension method.
            /// </param>
            /// <param name='cancellationToken'>
            /// The cancellation token.
            /// </param>
            public static async Task<object> GetSwaggerAsync(this ITenancyService operations, CancellationToken cancellationToken = default(CancellationToken))
            {
                using (var _result = await operations.GetSwaggerWithHttpMessagesAsync(null, cancellationToken).ConfigureAwait(false))
                {
                    return _result.Body;
                }
            }

    }
}