﻿using System.Net;
using Newtonsoft.Json.Linq;

namespace Marain.Tenancy.Specs.MultiHost
{
    public class TenancyResponse
    {
        public string? LocationHeader { get; set; }
        public string? EtagHeader { get; set; }
        public bool IsSuccessStatusCode { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string? CacheControlHeader { get; set; }
        public JObject? BodyJson { get; set; }
    }
}