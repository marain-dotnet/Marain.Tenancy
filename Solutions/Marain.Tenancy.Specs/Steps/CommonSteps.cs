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
public class CommonSteps
{
    public static Exception? LastException { get; set; }

    [Then("it should throw a {string}")]
    public void ThenItShouldThrowAnException(string exceptionTypeName)
    {
        Assert.AreEqual(exceptionTypeName, LastException?.GetType().Name);
    }

    [Then("it should throw an ApiException with Response Status Code {int}")]
    public void ThenTheApiExceptionShouldHaveStatusCode(int expectedStatusCode)
    {
        Assert.IsInstanceOf<ApiException>(LastException);
        var ex = (ApiException)LastException!;
        Assert.AreEqual(expectedStatusCode, ex.ResponseStatusCode);
    }

    public static void RethrowLastExceptionIfPresent()
    {
        if (LastException is not null)
        {
            var dispatchInfo = ExceptionDispatchInfo.Capture(LastException);
            dispatchInfo.Throw();
        }
    }
}