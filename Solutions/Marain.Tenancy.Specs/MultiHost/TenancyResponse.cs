﻿// <copyright file="TenancyResponse.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.MultiHost
{
    using System.Net;
    using System.Text.Json.Nodes;

    public class TenancyResponse
    {
        public string? LocationHeader { get; set; }

        public string? EtagHeader { get; set; }

        public bool IsSuccessStatusCode { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public string? CacheControlHeader { get; set; }

        public JsonObject? BodyJson { get; set; }
    }
}