// <copyright file="CommonSteps.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Specs.Steps;

using System;
using System.Runtime.ExceptionServices;
using Microsoft.Kiota.Abstractions;
using NUnit.Framework;
using Reqnroll;

[Binding]
public class CommonSteps : Steps
{
    private const string LastExceptionKey = "LastException";

    public static Exception? GetLastException(ScenarioContext context)
    {
        context.TryGetValue(LastExceptionKey, out Exception? ex);
        return ex;
    }

    public static void SetLastException(ScenarioContext context, Exception ex)
    {
        context.Set(ex, LastExceptionKey);
    }

    [Then("it should throw a {string}")]
    public void ThenItShouldThrowAnException(string exceptionTypeName)
    {
        Exception? lastException = GetLastException(this.ScenarioContext);
        Assert.AreEqual(exceptionTypeName, lastException?.GetType().Name);
    }

    [Then("it should throw an ApiException with Response Status Code {int}")]
    public void ThenTheApiExceptionShouldHaveStatusCode(int expectedStatusCode)
    {
        Exception? lastException = GetLastException(this.ScenarioContext);
        Assert.IsInstanceOf<ApiException>(lastException);
        var ex = (ApiException)lastException!;
        Assert.AreEqual(expectedStatusCode, ex.ResponseStatusCode);
    }

    public static void RethrowLastExceptionIfPresent(ScenarioContext context)
    {
        Exception? lastException = GetLastException(context);

        if (lastException is not null)
        {
            var dispatchInfo = ExceptionDispatchInfo.Capture(lastException);
            dispatchInfo.Throw();
        }
    }
}