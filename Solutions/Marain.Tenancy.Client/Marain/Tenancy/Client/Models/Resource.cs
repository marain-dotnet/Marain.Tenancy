// <auto-generated>
// Code generated by Microsoft (R) AutoRest Code Generator.
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>

namespace Marain.Tenancy.Client.Models
{
    using Newtonsoft.Json;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public partial class Resource
    {
        /// <summary>
        /// Initializes a new instance of the Resource class.
        /// </summary>
        public Resource()
        {
            CustomInit();
        }

        /// <summary>
        /// Initializes a new instance of the Resource class.
        /// </summary>
        /// <param name="_links">A hyperlink to a related URI.</param>
        public Resource(IDictionary<string, object> _links = default(IDictionary<string, object>), IDictionary<string, object> _embedded = default(IDictionary<string, object>))
        {
            this._links = _links;
            this._embedded = _embedded;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or sets a hyperlink to a related URI.
        /// </summary>
        /// <remarks>
        /// Represents a hyperlink from the containing resource to a URI.
        /// </remarks>
        [JsonProperty(PropertyName = "_links")]
        public IDictionary<string, object> _links { get; set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "_embedded")]
        public IDictionary<string, object> _embedded { get; set; }

    }
}
