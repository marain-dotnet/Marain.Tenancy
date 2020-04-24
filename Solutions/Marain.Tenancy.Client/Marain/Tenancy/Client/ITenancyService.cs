// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Marain.Tenancy.Client
{
    using Microsoft.Rest;
    using Models;
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// </summary>
    public partial interface ITenancyService : System.IDisposable
    {
        /// <summary>
        /// The base URI of the service.
        /// </summary>
        System.Uri BaseUri { get; set; }

        /// <summary>
        /// Gets or sets json serialization settings.
        /// </summary>
        JsonSerializerSettings SerializationSettings { get; }

        /// <summary>
        /// Gets or sets json deserialization settings.
        /// </summary>
        JsonSerializerSettings DeserializationSettings { get; }

        /// <summary>
        /// Subscription credentials which uniquely identify client
        /// subscription.
        /// </summary>
        ServiceClientCredentials Credentials { get; }


        /// <summary>
        /// Update a tenant
        /// </summary>
        /// <remarks>
        /// Updates the tenant
        /// </remarks>
        /// <param name='tenantId'>
        /// The tenant within which the request should operate
        /// </param>
        /// <param name='body'>
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> UpdateTenantWithHttpMessagesAsync(string tenantId, IList<UpdateTenantJsonPatchEntry> body, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets a tenant
        /// </summary>
        /// <remarks>
        /// Gets the tenant
        /// </remarks>
        /// <param name='tenantId'>
        /// The tenant within which the request should operate
        /// </param>
        /// <param name='ifNoneMatch'>
        /// The ETag of the last known version.
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<object,GetTenantHeaders>> GetTenantWithHttpMessagesAsync(string tenantId, string ifNoneMatch = default(string), Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get all child tenants of the current tenant
        /// </summary>
        /// <remarks>
        /// Get all child tenants of the current tenant
        /// </remarks>
        /// <param name='tenantId'>
        /// The tenant within which the request should operate
        /// </param>
        /// <param name='continuationToken'>
        /// A continuation token for an operation where more data is available
        /// </param>
        /// <param name='maxItems'>
        /// The maximum number of items to return in the request. Fewer than
        /// this number may be returned.
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> GetChildrenWithHttpMessagesAsync(string tenantId, string continuationToken = default(string), int? maxItems = default(int?), Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Create a child tenant
        /// </summary>
        /// <remarks>
        /// Creates a child tenant of the parent tenant
        /// </remarks>
        /// <param name='tenantId'>
        /// The tenant within which the request should operate
        /// </param>
        /// <param name='tenantName'>
        /// The name for the new tenant
        /// </param>
        /// <param name='wellKnownChildTenantGuid'>
        /// The well known Guid for the new tenant. If provided, this will be used to create the child tenant Id.
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationHeaderResponse<CreateChildTenantHeaders>> CreateChildTenantWithHttpMessagesAsync(string tenantId, string tenantName, System.Guid? wellKnownChildTenantGuid = default(System.Guid?), Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Delete a child tenant by ID
        /// </summary>
        /// <remarks>
        /// Deletes a child tenant of the parent tenant by ID
        /// </remarks>
        /// <param name='tenantId'>
        /// The tenant within which the request should operate
        /// </param>
        /// <param name='childTenantId'>
        /// The child tenant within the current tenant.
        /// </param>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse> DeleteChildTenantWithHttpMessagesAsync(string tenantId, string childTenantId, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// View swagger definition for this API
        /// </summary>
        /// <remarks>
        /// View swagger definition for this API
        /// </remarks>
        /// <param name='customHeaders'>
        /// The headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        Task<HttpOperationResponse<object>> GetSwaggerWithHttpMessagesAsync(Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken));

    }
}
