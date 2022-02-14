// <copyright file="DirectTestableTenancyService.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.MultiHost
{
    using System.Net;
    using System.Threading.Tasks;

    using Marain.Tenancy.OpenApi;

    using Menes;
    using Menes.Hal;

    using Newtonsoft.Json.Linq;

    internal class DirectTestableTenancyService : ITestableTenancyService
    {
        private readonly TenancyService tenancyService;
        private readonly IOpenApiContext openApiContext;

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

        private static TenancyResponse MakeResponse(OpenApiResult result)
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
                BodyJson = parsedResponse,
            };
        }
    }
}