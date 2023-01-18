// <copyright file="FunctionsStartupWrapper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.MultiHost
{
    using System.Linq;

    using Marain.Tenancy.Functions;
    using Marain.Tenancy.OpenApi;

    using Menes;
    using Menes.Internal;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    using TechTalk.SpecFlow.Assist;

    public class FunctionsStartupWrapper : IWebJobsStartup2
    {
        public void Configure(WebJobsBuilderContext context, IWebJobsBuilder builder)
        {
            Startup.Configure(builder.Services, context.Configuration);

            // Add services normally automatically present in the Functions Host that we rely on.
            // TODO: we shouldn't really need this, so see if we can work out how to live without it.
            builder.Services.AddSingleton(new TelemetryClient(new TelemetryConfiguration()));

            // Currently, the Menes test hosting is all based around the old model, so it doesn't
            // work if the function startup only registered the new-style HttpRequestData/HttpResponseData
            // hosting. So we add in the old style of hosting here for now.
            // TODO: we really need Menes to support the new style properly.
            builder.Services.AddOpenApiActionResultHosting<SimpleOpenApiContext>(config =>
            {
                config.Documents.RegisterOpenApiServiceWithEmbeddedDefinition<TenancyService>();
                config.Documents.AddSwaggerEndpoint();
            });

            builder.Services.Remove(builder.Services.First(svc => svc.ImplementationType == typeof(SwaggerService)));
        }

        public void Configure(IWebJobsBuilder builder)
        {
        }
    }
}