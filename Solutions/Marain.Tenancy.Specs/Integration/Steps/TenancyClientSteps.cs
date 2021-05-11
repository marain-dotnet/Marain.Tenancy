namespace Marain.Tenancy.Specs.Integration.Steps
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Corvus.Extensions.Json;
    using Corvus.Tenancy;
    using Corvus.Tenancy.Exceptions;
    using Corvus.Testing.SpecFlow;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using TechTalk.SpecFlow;

    [Binding]
    public class TenancyClientSteps
    {
        private readonly FeatureContext featureContext;
        private readonly ScenarioContext scenarioContext;
        private readonly ITenantStore store;
        private readonly IJsonNetPropertyBagFactory propertyBagFactory;

        public TenancyClientSteps(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            this.featureContext = featureContext;
            this.scenarioContext = scenarioContext;

            this.store = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<ITenantStore>();
            this.propertyBagFactory = ContainerBindings.GetServiceProvider(this.featureContext).GetRequiredService<IJsonNetPropertyBagFactory>();
        }

        [Given("I get the tenant id of the tenant called \"(.*)\" and call it \"(.*)\"")]
        [When("I get the tenant id of the tenant called \"(.*)\" and call it \"(.*)\"")]
        public void WhenIGetTheTenantIdOfTheTenantCalledAndCallIt(string tenantName, string tenantIdName)
        {
            ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);
            this.scenarioContext.Set(tenant.Id, tenantIdName);
        }

        [Given("I get the tenant with the id called \"(.*)\" and call it \"(.*)\"")]
        [When("I get the tenant with the id called \"(.*)\" and call it \"(.*)\"")]
        public async Task WhenIGetTheTenantWithTheIdCalled(string tenantIdName, string tenantName)
        {
            ITenant tenant = await this.store.GetTenantAsync(this.scenarioContext.Get<string>(tenantIdName)).ConfigureAwait(false);
            this.scenarioContext.Set(tenant, tenantName);
        }

        [When(@"I get the tenant with id ""(.*)"" and call it ""(.*)""")]
        public async Task WhenIGetTheTenantWithIdAndCallItAsync(string tenantId, string tenantName)
        {
            ITenant tenant = await this.store.GetTenantAsync(tenantId).ConfigureAwait(false);
            this.scenarioContext.Set(tenant, tenantName);
        }

        [Then("the tenant called \"(.*)\" should have the same ID as the tenant called \"(.*)\"")]
        public void ThenTheTenantCalledShouldHaveTheSameIDAsTheTenantCalled(string firstName, string secondName)
        {
            ITenant firstTenant = this.scenarioContext.Get<ITenant>(firstName);
            ITenant secondTenant = this.scenarioContext.Get<ITenant>(secondName);
            Assert.AreEqual(firstTenant.Id, secondTenant.Id);
        }

        [Then(@"the tenant called ""(.*)"" should now have the name ""(.*)""")]
        public void ThenTheTenantCalledShouldNowHaveTheName(
            string nameTenantStoredUnder, string newTenantName)
        {
            ITenant tenant = this.scenarioContext.Get<ITenant>(nameTenantStoredUnder);
            Assert.AreEqual(newTenantName, tenant.Name);
        }

        [Then("the tenant called \"(.*)\" should have no properties")]
        public void ThenTheTenantCalledShouldHaveNoProperties(string tenantName)
        {
            ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);

            JObject properties = this.propertyBagFactory.AsJObject(tenant.Properties);
            Assert.AreEqual(0, properties.Count);
        }

        [Then("the tenant called \"(.*)\" should have the properties")]
        public void ThenTheTenantCalledShouldHaveTheProperties(string tenantName, Table table)
        {
            ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);

            foreach (TableRow row in table.Rows)
            {
                row.TryGetValue("Key", out string key);
                row.TryGetValue("Value", out string value);
                row.TryGetValue("Type", out string type);
                switch (type)
                {
                    case "string":
                        {
                            Assert.IsTrue(tenant.Properties.TryGet(key, out string actual), $"Property {key} should be present");
                            Assert.AreEqual(value, actual);
                            break;
                        }

                    case "integer":
                        {
                            Assert.IsTrue(tenant.Properties.TryGet(key, out int actual), $"Property {key} should be present");
                            Assert.AreEqual(int.Parse(value), actual);
                            break;
                        }

                    case "datetimeoffset":
                        {
                            Assert.IsTrue(tenant.Properties.TryGet(key, out DateTimeOffset actual), $"Property {key} should be present");
                            Assert.AreEqual(DateTimeOffset.Parse(value), actual);
                            break;
                        }

                    default:
                        throw new InvalidOperationException($"Unknown data type '{type}'");
                }
            }
        }

        [Given("I create a child tenant called \"(.*)\" for the root tenant")]
        public async Task GivenICreateAChildTenantCalledForTheRootTenant(string tenantName)
        {
            ITenant tenant = await this.store.CreateChildTenantAsync(RootTenant.RootTenantId, tenantName).ConfigureAwait(false);
            this.scenarioContext.Set(tenant, tenantName);
        }

        [Given("I create a child tenant called \"(.*)\" for the tenant called \"(.*)\"")]
        public async Task GivenICreateAChildTenantCalledForTheTenantCalled(string childName, string parentName)
        {
            ITenant parentTenant = this.scenarioContext.Get<ITenant>(parentName);
            ITenant tenant = await this.store.CreateChildTenantAsync(parentTenant.Id, childName).ConfigureAwait(false);
            this.scenarioContext.Set(tenant, childName);
        }

        [Given("I update the properties of the tenant called \"(.*)\"")]
        [When("I update the properties of the tenant called \"(.*)\"")]
        public void WhenIUpdateThePropertiesOfTheTenantCalled(string tenantName, Table table)
        {
            ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);
            var propertiesToSetOrAdd = new Dictionary<string, object>();

            foreach (TableRow row in table.Rows)
            {
                row.TryGetValue("Key", out string key);
                row.TryGetValue("Value", out string value);
                row.TryGetValue("Type", out string type);
                switch (type)
                {
                    case "string":
                        {
                            propertiesToSetOrAdd.Add(key, value);
                            break;
                        }

                    case "integer":
                        {
                            propertiesToSetOrAdd.Add(key, int.Parse(value));
                            break;
                        }

                    case "datetimeoffset":
                        {
                            propertiesToSetOrAdd.Add(key, DateTimeOffset.Parse(value));
                            break;
                        }

                    default:
                        throw new InvalidOperationException($"Unknown data type '{type}'");
                }
            }

            this.store.UpdateTenantAsync(tenant.Id, propertiesToSetOrAdd: propertiesToSetOrAdd);
        }

        [When(@"I rename the tenant called ""(.*)"" to ""(.*)"" and update its properties")]
        public async Task WhenIRenameTheTenantCalledToAndUpdateItsPropertiesAsync(
            string tenantName,
            string newTenantName,
            Table table)
        {
            ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);

            var propertiesToRemove = new List<string>();
            var propertiesToSetOrAdd = new Dictionary<string, object>();
            foreach (TableRow row in table.Rows)
            {
                string propertyName = row["Property"];
                switch (row["Action"])
                {
                    case "remove":
                        propertiesToRemove.Add(propertyName);
                        break;

                    case "addOrSet":
                        string value = row["Value"];
                        string type = row["Type"];
                        object actualValue = type switch
                        {
                            "string" => value,
                            "integer" => int.Parse(value),
                            _ => throw new InvalidOperationException($"Unknown data type '{type}'")
                        };
                        propertiesToSetOrAdd.Add(propertyName, actualValue);
                        break;

                    default:
                        Assert.Fail("Unknown action in add/modify/remove table: " + row["Action"]);
                        break;
                }
            }

            await this.store.UpdateTenantAsync(
                tenant.Id,
                newTenantName,
                propertiesToSetOrAdd.Count == 0 ? null : propertiesToSetOrAdd,
                propertiesToRemove.Count == 0 ? null : propertiesToRemove).ConfigureAwait(false);
        }

        [When(@"I try to update the properties of the tenant with id ""(.*)""")]
        public async Task WhenITryToUpdateThePropertiesOfTheTenantWithIdAsync(string tenantId)
        {
            ITenant tenant = await this.store.GetTenantAsync(tenantId).ConfigureAwait(false);
            var propertiesToAdd = new Dictionary<string, object> { { "foo", "bar" } };
            try
            {
                await this.store.UpdateTenantAsync(tenant.Id, propertiesToSetOrAdd: propertiesToAdd).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Set(ex);
            }
        }

        [When("I get the children of the tenant with the id called \"(.*)\" with maxItems (.*) and call them \"(.*)\"")]
        public async Task WhenIGetTheChildrenOfTheTenantWithTheIdCalledWithMaxItemsAndCallThem(string tenantIdName, int maxItems, string childrenName)
        {
            string tenantId = this.scenarioContext.Get<string>(tenantIdName);
            TenantCollectionResult children = await this.store.GetChildrenAsync(tenantId, maxItems).ConfigureAwait(false);
            this.scenarioContext.Set(children, childrenName);
        }

        [When("I get the children of the tenant with the id called \"(.*)\" with maxItems (.*) and continuation token \"(.*)\" and call them \"(.*)\"")]
        public async Task WhenIGetTheChildrenOfTheTenantWithTheIdCalledWithMaxItemsAndCallThem(string tenantIdName, int maxItems, string continuationTokenSource, string childrenName)
        {
            string tenantId = this.scenarioContext.Get<string>(tenantIdName);
            TenantCollectionResult previousChildren = this.scenarioContext.Get<TenantCollectionResult>(continuationTokenSource);
            TenantCollectionResult children = await this.store.GetChildrenAsync(tenantId, maxItems, previousChildren.ContinuationToken).ConfigureAwait(false);
            this.scenarioContext.Set(children, childrenName);
        }

        [Then("the ids of the children called \"(.*)\" should match the ids of the tenants called")]
        public void ThenTheIdsOfTheChildrenCalledShouldMatchTheIdsOfTheTenantsCalled(string childrenName, Table table)
        {
            TenantCollectionResult children = this.scenarioContext.Get<TenantCollectionResult>(childrenName);
            Assert.AreEqual(table.Rows.Count, children.Tenants.Count);
            var expected = table.Rows.Select(r => this.scenarioContext.Get<ITenant>(r[0]).Id).ToList();
            CollectionAssert.AreEquivalent(expected, children.Tenants);
        }

        [Then("there should be no ids in the children called \"(.*)\"")]
        public void ThenThereShouldBeNoIdsInTheChildrenCalled(string childrenName)
        {
            TenantCollectionResult children = this.scenarioContext.Get<TenantCollectionResult>(childrenName);
            Assert.AreEqual(0, children.Tenants.Count);
        }

        [Then("there should be (.*) tenants in \"(.*)\"")]
        public void ThenThereShouldBeTenantsIn(int count, string childrenName)
        {
            TenantCollectionResult children = this.scenarioContext.Get<TenantCollectionResult>(childrenName);
            Assert.AreEqual(count, children.Tenants.Count);
        }

        [Then(@"the ids of the children called ""(.*)"" and ""(.*)"" should each match (.*) of the ids of the tenants called")]
        public void ThenTheIdsOfTheChildrenCalledAndShouldEachMatchOfTheIdsOfTheTenantsCalled(string childrenName1, string childrenName2, int count, Table table)
        {
            TenantCollectionResult children1 = this.scenarioContext.Get<TenantCollectionResult>(childrenName1);
            TenantCollectionResult children2 = this.scenarioContext.Get<TenantCollectionResult>(childrenName2);
            Assert.AreEqual(count, children1.Tenants.Count);
            Assert.AreEqual(count, children2.Tenants.Count);
            var expected = table.Rows.Select(r => this.scenarioContext.Get<ITenant>(r[0]).Id).ToList();
            CollectionAssert.AreEquivalent(expected, children1.Tenants.Union(children2.Tenants));
        }

        [When("I delete the tenant with the id called \"(.*)\"")]
        public Task WhenIDeleteTheTenantWithTheIdCalled(string tenantIdName)
        {
            string tenantId = this.scenarioContext.Get<string>(tenantIdName);
            return this.store.DeleteTenantAsync(tenantId);
        }

        [When("I get a tenant with id \"(.*)\"")]
        public async Task WhenIGetATenantWithId(string tenantId)
        {
            try
            {
                await this.store.GetTenantAsync(tenantId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Set(ex);
            }
        }

        [Then("it should throw a TenantNotFoundException")]
        public void ThenItShouldThrowATenantNotFoundException()
        {
            Assert.IsInstanceOf<TenantNotFoundException>(this.scenarioContext.Get<Exception>());
        }

        [Given(@"I get the ETag of the tenant called ""(.*)"" and call it ""(.*)""")]
        public void GivenIGetTheETagOfTheTenantCalledAndCallIt(string tenantName, string eTagName)
        {
            ITenant tenant = this.scenarioContext.Get<ITenant>(tenantName);
            this.scenarioContext.Set(tenant.ETag, eTagName);
        }

        [When(@"I get the tenant with the id called ""(.*)"" and the ETag called ""(.*)""")]
        public async Task WhenIGetTheTenantWithTheIdCalledAndTheETagCalled(string tenantIdName, string tenantETagName)
        {
            try
            {
                ITenant tenant = await this.store.GetTenantAsync(this.scenarioContext.Get<string>(tenantIdName), this.scenarioContext.Get<string>(tenantETagName)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.scenarioContext.Set(ex);
            }
        }

        [Then("it should throw a TenantNotModifiedException")]
        public void ThenItShouldThrowATenantNotModifiedException()
        {
            Assert.IsInstanceOf<TenantNotModifiedException>(this.scenarioContext.Get<Exception>());
        }

        [Then("it should throw a NotSupportedException")]
        public void ThenItShouldThrowANotSupportedException()
        {
            Assert.IsInstanceOf<NotSupportedException>(this.scenarioContext.Get<Exception>());
        }
    }
}
