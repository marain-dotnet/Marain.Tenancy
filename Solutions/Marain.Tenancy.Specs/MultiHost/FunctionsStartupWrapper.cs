// <copyright file="FunctionsStartupWrapper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.MultiHost
{
    using Marain.Tenancy.ControlHost;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;

    public class FunctionsStartupWrapper : FunctionsStartup
    {
        private readonly Startup wrappedStartup = new Startup();

        public override void Configure(IFunctionsHostBuilder builder)
        {
            this.wrappedStartup.Configure(builder);

            // Add services normally automatically present in the Functions Host that we rely on.
            // TODO: we shouldn't really need this, so see if we can work out how to live without it.
            builder.Services.AddSingleton(new TelemetryClient(new TelemetryConfiguration()));
        }
    }
}