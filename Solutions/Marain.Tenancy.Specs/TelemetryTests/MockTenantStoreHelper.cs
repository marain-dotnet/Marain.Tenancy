// <copyright file="MockTenantStoreHelper.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.TelemetryTests;

using System;
using System.Threading.Tasks;
using Corvus.Tenancy;
using Moq;

/// <summary>
/// Helper class for creating mock tenant store for testing.
/// </summary>
internal static class MockTenantStoreHelper
{
    /// <summary>
    /// Creates a mock tenant store for integration testing.
    /// </summary>
    /// <returns>A mock ITenantStore instance.</returns>
    public static ITenantStore CreateMockTenantStore()
    {
        // This would typically return a test implementation
        // For now, return a substitute that throws appropriate exceptions
        Mock<ITenantStore> mockStore = new();

        mockStore.Setup(x => x.GetTenantAsync(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.FromException<ITenant>(new ArgumentException("Tenant not found")));

        mockStore.Setup(x => x.CreateWellKnownChildTenantAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(Task.FromException<ITenant>(new InvalidOperationException("Cannot create tenant in test environment")));

        return mockStore.Object;
    }
}