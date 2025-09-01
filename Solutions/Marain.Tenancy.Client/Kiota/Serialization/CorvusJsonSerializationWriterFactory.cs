// <copyright file="CorvusJsonSerializationWriterFactory.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Kiota.Serialization;

using System;
using System.IO;
using System.Text.Json;
using Corvus.Json.Serialization;
using Microsoft.Kiota.Abstractions.Serialization;

/// <summary>
/// A custom serialization writer factory that uses Corvus JSON serialization options.
/// </summary>
public class CorvusJsonSerializationWriterFactory : ISerializationWriterFactory
{
    private readonly IJsonSerializerOptionsProvider _optionsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorvusJsonSerializationWriterFactory"/> class.
    /// </summary>
    /// <param name="optionsProvider">The JSON serializer options provider.</param>
    public CorvusJsonSerializationWriterFactory(IJsonSerializerOptionsProvider optionsProvider)
    {
        this._optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
    }

    /// <summary>
    /// Gets the valid content type for this factory.
    /// </summary>
    public string ValidContentType => "application/json";

    /// <summary>
    /// Creates a serialization writer for the specified content type.
    /// </summary>
    /// <param name="contentType">The content type to serialize to.</param>
    /// <returns>A serialization writer instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when contentType is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when contentType is not supported.</exception>
    public ISerializationWriter GetSerializationWriter(string contentType)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentType);

        if (!this.ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentOutOfRangeException(nameof(contentType), $"Expected {this.ValidContentType}");
        }

        var stream = new MemoryStream();
        JsonSerializerOptions jsonOptions = this._optionsProvider.Instance;
        var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Encoder = jsonOptions.Encoder,
            Indented = jsonOptions.WriteIndented,
            SkipValidation = false,
        });

        return new CorvusJsonSerializationWriter(writer, jsonOptions, stream);
    }
}