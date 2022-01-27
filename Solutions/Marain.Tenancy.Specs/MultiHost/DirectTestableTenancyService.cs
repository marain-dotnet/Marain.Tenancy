using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Marain.Tenancy.OpenApi;
using Menes;
using Menes.Hal;
using Moq;
using Newtonsoft.Json.Linq;

namespace Marain.Tenancy.Specs.MultiHost
{
    internal class DirectTestableTenancyService : ITestableTenancyService
    {
        private TenancyService tenancyService;
        private IOpenApiContext openApiContext;

        public DirectTestableTenancyService(TenancyService tenancyService, IOpenApiContext openApiContext)
        {
            this.tenancyService = tenancyService;
            this.openApiContext = openApiContext;
        }

        public async Task<TenancyResponse> CreateTenantAsync(string parentId, string name)
        {
            OpenApiResult result = await this.tenancyService.CreateChildTenantAsync(parentId, name, null, this.openApiContext);
            return MakeResponse(result);
        }

        public Task<TenancyResponse> GetSwaggerAsync()
        {
            OpenApiResult result = this.tenancyService.NotFoundResult();
            // TODO: Fix if statement to check for swagger endpoint
            if (this.openApiContext != null)
            {
                result = this.tenancyService.OkResult();
            }
            return Task.FromResult(MakeResponse(result));
        }

        public async Task<TenancyResponse> GetTenantAsync(string tenantId, string? etag)
        {
            OpenApiResult result = await this.tenancyService.GetTenantAsync(tenantId, etag, this.openApiContext);
            return MakeResponse(result);
        }

        public async Task<TenancyResponse> GetTenantByLocationAsync(string location)
        {
            string locationId = location[1..location.IndexOf('/', 1)];
            OpenApiResult result = await this.tenancyService.GetTenantAsync(locationId, null, this.openApiContext);
            return MakeResponse(result);
        }

        private TenancyResponse MakeResponse(OpenApiResult result)
        {
            JObject? parsedResponse = null;
            if (result.Results.TryGetValue("application/json", out object? document))
            {
                var halDocument = document as HalDocument;
                parsedResponse = halDocument?.Properties!;
            }

            return new TenancyResponse
            {
                LocationHeader = (result.Results.TryGetValue("Location", out object? location) ? location : "") as string,
                EtagHeader = (result.Results.TryGetValue("ETag", out object? etag) ? etag : null) as string,
                IsSuccessStatusCode = result.StatusCode >= 200 && result.StatusCode < 300,
                StatusCode = (HttpStatusCode)result.StatusCode,
                CacheControlHeader = (result.Results.TryGetValue("Cache-Control", out object? cacheControl) ? cacheControl : "") as string,
                BodyJson = parsedResponse
            };
        }

    }
}
