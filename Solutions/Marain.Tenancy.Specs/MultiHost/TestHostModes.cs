// <copyright file="TestHostModes.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.MultiHost;

/// <summary>
/// Service hosting mechanisms for which test suites may be executed.
/// </summary>
/// <remarks>
/// This enum defines the available test hosting modes for the Tenancy service.
/// The service can be tested through MinimalApi in-process hosting or via the generated client.
/// </remarks>
public enum TestHostModes
{
    /// <summary>
    /// Host the service in-process using ASP.NET Core MinimalApi with TestServer.
    /// </summary>
    InProcessMinimalApi,

    /// <summary>
    /// Test the service through the generated HTTP client.
    /// </summary>
    TenancyClient,
}