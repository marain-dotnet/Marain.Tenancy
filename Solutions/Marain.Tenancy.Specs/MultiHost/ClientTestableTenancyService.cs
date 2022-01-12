using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Marain.Tenancy.Specs.MultiHost
{
    internal class ClientTestableTenancyService : ITestableTenancyService
    {
        private static readonly HttpClient HttpClient = new();
        private HttpResponseMessage? response;
        private string? responseContent;
        private string tenancyApiBaseUriText;
        private JsonSerializerSettings instance;
        private JObject? parsedResponse;

        public ClientTestableTenancyService(string tenancyApiBaseUriText, JsonSerializerSettings instance)
        {
            this.tenancyApiBaseUriText = tenancyApiBaseUriText;
            this.instance = instance;
        }

        public async Task<TenancyResponse> GetTenantAsync(string tenantId)
        {
            await this.SendGetRequest(new (this.tenancyApiBaseUriText), $"/{tenantId}/marain/tenant");
            return new TenancyResponse();
        }

        private async Task SendGetRequest(Uri baseUri, string path, string? etag = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, path));

            if (!string.IsNullOrEmpty(etag))
            {
                request.Headers.Add("If-None-Match", etag);
            }

            this.response = await HttpClient.SendAsync(request).ConfigureAwait(false);
            this.responseContent = await this.response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (this.response.IsSuccessStatusCode && !string.IsNullOrEmpty(this.responseContent))
            {
                this.parsedResponse = JObject.Parse(this.responseContent);
            }
        }
    }
}
