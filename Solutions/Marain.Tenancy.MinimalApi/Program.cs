// <copyright file="Program.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

using Marain.Tenancy.MinimalApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddTenancyMinimalApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.MapTenancyEndpoints();

app.Run();