// <copyright file="JsonSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Integration.Steps
{
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    using NUnit.Framework;
    using NUnit.Framework.Internal;

    using TechTalk.SpecFlow;

    [Binding]
    public class JsonSteps : Steps
    {
        public JsonObject? Json { get; internal set; }

        [Then("the response content should have a property called '(.*)'")]
        public void ThenTheResponseObjectShouldHaveAPropertyCalled(string propertyPath)
        {
            this.GetRequiredTokenFromResponseObject(propertyPath);
        }

        [Then("the response content should have a string property called '(.*)' with value '(.*)'")]
        public void ThenTheResponseObjectShouldHaveAStringPropertyCalledWithValue(string propertyPath, string expectedValue)
        {
            JsonNode actualToken = this.GetRequiredTokenFromResponseObject(propertyPath);

            string? actualValue = actualToken.GetValue<string>();
            Assert.AreEqual(expectedValue, actualValue, $"Expected value of property '{propertyPath}' was '{expectedValue}', but actual value was '{actualValue}'");
        }

        [Then("the response content should have a boolean property called '(.*)' with value '(.*)'")]
        public void ThenTheResponseContentShouldHaveABooleanPropertyCalledWithValue(string propertyPath, bool expectedValue)
        {
            JsonNode actualToken = this.GetRequiredTokenFromResponseObject(propertyPath);

            bool actualValue = actualToken.GetValue<bool>();
            Assert.AreEqual(expectedValue, actualValue, $"Expected value of property '{propertyPath}' was '{expectedValue}', but actual value was '{actualValue}'");
        }

        [Then("the response content should have a date-time property called '(.*)' with value '(.*)'")]
        public void ThenTheResponseContentShouldHaveADate_TimePropertyCalledWithValue(string propertyPath, DateTimeOffset expectedValue)
        {
            JsonNode actualToken = this.GetRequiredTokenFromResponseObject(propertyPath);

            DateTimeOffset actualValue = actualToken.Deserialize<DateTimeOffset>();
            Assert.AreEqual(expectedValue, actualValue, $"Expected value of property '{propertyPath}' was '{expectedValue}', but actual value was '{actualValue}'");
        }

        [Then("the response content should have a long property called '(.*)' with value (.*)")]
        public void ThenTheResponseObjectShouldHaveALongPropertyCalledWithValue(string propertyPath, long expectedValue)
        {
            JsonNode actualToken = this.GetRequiredTokenFromResponseObject(propertyPath);

            long actualValue = actualToken.GetValue<long>();
            Assert.AreEqual(expectedValue, actualValue, $"Expected value of property '{propertyPath}' was {expectedValue}, but actual value was {actualValue}");
        }

        [Then("the response content should not have a property called '(.*)'")]
        public void ThenTheResponseObjectShouldNotHaveAPropertyCalled(string propertyPath)
        {
            JsonObject data = this.ScenarioContext.Get<JsonObject>();
            Assert.False(
                TryGetToken(data, propertyPath, out _),
                $"Expected not to find a property with path '{propertyPath}', but one was present.");
        }

        [Then("the response content should have an array property called '(.*)' containing (.*) entries")]
        public void ThenTheResponseObjectShouldHaveAnArrayPropertyCalledContainingEntries(string propertyPath, int expectedEntryCount)
        {
            JsonNode actualToken = this.GetRequiredTokenFromResponseObject(propertyPath);
            JsonArray tokenArray = actualToken.AsArray();
            Assert.AreEqual(expectedEntryCount, tokenArray.Count, $"Expected array '{propertyPath}' to contain {expectedEntryCount} elements but found {tokenArray.Count}.");
        }

        [Then("each item in the response content array property called '(.*)' should have a property called '(.*)'")]
        public void ThenEachItemInTheResponseContentArrayPropertyCalledShouldHaveAPropertyCalled(string arrayPropertyPath, string itemPropertyPath)
        {
            JsonNode actualToken = this.GetRequiredTokenFromResponseObject(arrayPropertyPath);

            foreach (JsonNode? current in actualToken.AsArray())
            {
                GetRequiredToken((JsonObject)current!, itemPropertyPath);
            }
        }

        [Given("I have stored the value of the response object property called '(.*)' as '(.*)'")]
        public void GivenIHaveStoredTheValueOfTheResponseObjectPropertyCalledAs(string propertyPath, string storeAsName)
        {
            JsonNode token = this.GetRequiredTokenFromResponseObject(propertyPath);
            string? valueAsString = token.GetValue<string>();
            this.ScenarioContext.Set(valueAsString, storeAsName);
        }

        [Then("the response content should have a json property called '(.*)' with value '(.*)'")]
        public void ThenTheResponseContentShouldHaveAJsonPropertyCalledWithValue(string propertyPath, string storeAsJson)
        {
            JsonNode token = this.GetRequiredTokenFromResponseObject(propertyPath);
            string valueAsString = token.ToString()
                .Replace("\r\n ", string.Empty)
                .Replace("\r\n", " ");

            Assert.AreEqual(storeAsJson, valueAsString);
        }

        public JsonNode GetRequiredTokenFromResponseObject(string propertyPath)
        {
            JsonObject data = this.Json ?? throw new InvalidOperationException("Json not present");
            return GetRequiredToken(data, propertyPath);
        }

        public static JsonNode GetRequiredToken(JsonObject data, string propertyPath)
        {
            JsonNode? result = data;
            string pathSoFar = "";
            foreach (string propertyName in propertyPath.Split('.'))
            {
                var currentObject = (JsonObject)result!;
                Assert.IsTrue(
                    currentObject.TryGetPropertyValue(propertyName, out result),
                    $"Could not locate a property with path '{propertyPath}'. Got as far as '{pathSoFar}', but couldn't find '{propertyName}'");
                pathSoFar = pathSoFar.Length == 0
                    ? propertyName
                    : $".{propertyName}";
            }

            return result!;
        }

        public static bool TryGetToken(JsonObject data, string propertyPath, out JsonNode? node)
        {
            JsonNode? currentNode = data;
            foreach (string propertyName in propertyPath.Split('.'))
            {
                if (currentNode is not JsonObject currentObject ||
                    !currentObject.TryGetPropertyValue(propertyName, out currentNode))
                {
                    node = null;
                    return false;
                }
            }

            node = currentNode;
            return true;
        }
    }
}