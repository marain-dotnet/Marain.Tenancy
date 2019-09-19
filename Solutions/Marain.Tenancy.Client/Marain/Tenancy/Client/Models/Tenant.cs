// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Marain.Tenancy.Client.Models
{
    using Microsoft.Rest;
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public partial class Tenant : Resource
    {
        /// <summary>
        /// Initializes a new instance of the Tenant class.
        /// </summary>
        public Tenant()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the Tenant class.
        /// </summary>
        /// <param name="id">The unique ID of the tenant. This forms a path
        /// with parent tenants.</param>
        /// <param name="contentType">The content type of the tenant.</param>
        /// <param name="_links">Hyperlink</param>
        public Tenant(string id, string contentType, IDictionary<string, object> _links = default(IDictionary<string, object>), IDictionary<string, object> _embedded = default(IDictionary<string, object>), string eTag = default(string), IDictionary<string, object> properties = default(IDictionary<string, object>))
            : base(_links, _embedded)
        {
            Id = id;
            ETag = eTag;
            ContentType = contentType;
            Properties = properties;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets the unique ID of the tenant. This forms a path with
        /// parent tenants.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "eTag")]
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets the content type of the tenant.
        /// </summary>
        [JsonProperty(PropertyName = "contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public IDictionary<string, object> Properties { get; set; }

        /// <summary>
        /// Validate the object.
        /// </summary>
        /// <exception cref="ValidationException">
        /// Thrown if validation fails
        /// </exception>
        public virtual void Validate()
        {
            if (Id == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "Id");
            }
            if (ContentType == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "ContentType");
            }
        }
    }
}
