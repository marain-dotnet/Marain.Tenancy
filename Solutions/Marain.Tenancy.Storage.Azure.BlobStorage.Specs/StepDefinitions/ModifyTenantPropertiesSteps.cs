// <copyright file="ModifyTenantPropertiesSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Storage.Azure.BlobStorage.Specs.StepDefinitions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Corvus.Tenancy;

    using FluentAssertions;

    using Marain.Tenancy.Storage.Azure.BlobStorage.Specs.Bindings;

    using TechTalk.SpecFlow;

    [Binding]
    public class ModifyTenantPropertiesSteps : TenantStepsBase
    {
        public ModifyTenantPropertiesSteps(TenantProperties tenantProperties)
            : base(tenantProperties)
        {
        }

        [When("I update the properties of the tenant labelled '([^']*)' and label the returned tenant '([^']*)'")]
        public async Task GivenIUpdateTenantProperties(
            string tenantLabel, string updatedTenantLabel, Table propertyTable)
        {
            IEnumerable<KeyValuePair<string, object>> properties = ReadPropertiesTable(propertyTable);
            ITenant existingTenant = this.Tenants[tenantLabel];
            ITenant updatedTenant = await this.TenantStore.UpdateTenantAsync(existingTenant.Id, propertiesToSetOrAdd: properties);
            this.Tenants.Add(updatedTenantLabel, updatedTenant);
        }

        [When("I remove the '(.*)' property of the tenant labelled '(.*)' and label the returned tenant '(.*)'")]
        public async Task WhenIRemoveThePropertyOfTheTenantLabelledAndLabelTheReturnedTenantAsync(
            string propertykey, string existingTenantLabel, string updatedTenantLabel)
        {
            ITenant existingTenant = this.Tenants[existingTenantLabel];
            ITenant updatedTenant = await this.TenantStore.UpdateTenantAsync(
                existingTenant.Id,
                propertiesToRemove: new[] { propertykey });
            this.Tenants.Add(updatedTenantLabel, updatedTenant);
        }

        [When("I update the properties of the tenant labelled '(.*)' and remove the '(.*)' property and call the returned tenant '(.*)'")]
        public async Task WhenIUpdateThePropertiesOfTheTenantLabelledAndRemoveThePropertyAndCallTheReturnedTenantAsync(
            string existingTenantLabel, string propertykey, string updatedTenantLabel, Table propertyTable)
        {
            IEnumerable<KeyValuePair<string, object>> properties = ReadPropertiesTable(propertyTable);
            ITenant existingTenant = this.Tenants[existingTenantLabel];
            ITenant updatedTenant = await this.TenantStore.UpdateTenantAsync(
                existingTenant.Id,
                propertiesToSetOrAdd: properties,
                propertiesToRemove: new[] { propertykey });
            this.Tenants.Add(updatedTenantLabel, updatedTenant);
        }

        [Then("the tenant labelled '([^']*)' should have the properties")]
        public void ThenTheTenantCalledShouldHaveTheProperties(string tenantLabel, Table propertyTable)
        {
            ITenant tenant = this.Tenants[tenantLabel];
            foreach ((string key, object expectedValue) in ReadPropertiesTable(propertyTable))
            {
                // We need to get the properties from the tenant specifying the type required because
                // it may need to do specific serialization work.
                switch (expectedValue)
                {
                    case string s:
                        tenant.Properties.TryGet(key, out string? stringValue).Should().BeTrue();
                        stringValue.Should().Be(s);
                        break;

                    case int i:
                        tenant.Properties.TryGet(key, out int intValue).Should().BeTrue();
                        intValue.Should().Be(i);
                        break;

                    case DateTimeOffset dto:
                        tenant.Properties.TryGet(key, out DateTimeOffset dateTimeOffsetValue).Should().BeTrue();
                        dateTimeOffsetValue.Should().Be(dto);
                        break;
                }
            }
        }
    }
}