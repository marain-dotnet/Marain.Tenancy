// <copyright file="WebLinkListJsonConverter.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client.Serialization;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marain.Clients.Hal;

/// <summary>
/// Custom JSON converter for List&lt;WebLink&gt;? that handles deserialization of both
/// single WebLink objects and arrays of WebLink objects for backwards compatibility.
/// </summary>
public class WebLinkListJsonConverter : JsonConverter<List<WebLink>?>
{
    /// <inheritdoc/>
    public override List<WebLink>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Single WebLink object - deserialize and wrap in a list
            WebLink? webLink = JsonSerializer.Deserialize<WebLink>(ref reader, options);
            return webLink != null ? [webLink] : null;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            // Array of WebLink objects - deserialize normally
            return JsonSerializer.Deserialize<List<WebLink>>(ref reader, options);
        }

        throw new JsonException($"Unexpected token type: {reader.TokenType}. Expected StartObject, StartArray, or Null.");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, List<WebLink>? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Always serialize as an array for consistency
        JsonSerializer.Serialize(writer, value, options);
    }
}