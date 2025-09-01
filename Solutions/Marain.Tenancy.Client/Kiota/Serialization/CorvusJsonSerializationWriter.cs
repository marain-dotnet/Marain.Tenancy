// <copyright file="CorvusJsonSerializationWriter.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Kiota.Serialization;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

/// <summary>
/// A custom JSON serialization writer that uses Corvus JSON serialization options.
/// </summary>
public class CorvusJsonSerializationWriter : ISerializationWriter, IDisposable
{
    private readonly Utf8JsonWriter writer;
    private readonly JsonSerializerOptions options;
    private readonly MemoryStream stream;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorvusJsonSerializationWriter"/> class.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="options">The JSON serializer options.</param>
    /// <param name="stream">The underlying stream.</param>
    public CorvusJsonSerializationWriter(Utf8JsonWriter writer, JsonSerializerOptions options, MemoryStream stream)
    {
        this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    /// <summary>
    /// Gets the serialized content as a stream.
    /// </summary>
    /// <returns>A stream containing the serialized content.</returns>
    public Stream GetSerializedContent()
    {
        this.writer.Flush();

        MemoryStream copy = new(this.stream.ToArray());
        return copy;
    }

    /// <summary>
    /// Writes a string value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The string value to write.</param>
    public void WriteStringValue(string? key, string? value)
    {
        if (key != null)
        {
            this.writer.WriteString(key, value);
        }
        else
        {
            this.writer.WriteStringValue(value);
        }
    }

    /// <summary>
    /// Writes an object value.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="key">The property key.</param>
    /// <param name="value">The object value to write.</param>
    /// <param name="additionalValuesToMerge">Additional values to merge.</param>
    public void WriteObjectValue<T>(string? key, T? value, params IParsable?[] additionalValuesToMerge)
        where T : IParsable
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        // Special handling for UntypedNode - serialize the underlying value directly
        if (value is UntypedNode untypedNode)
        {
            this.WriteUntypedNodeValue(key, untypedNode);
            return;
        }

        if (key != null)
        {
            this.writer.WriteStartObject(key);
        }
        else
        {
            this.writer.WriteStartObject();
        }

        value.Serialize(this);

        if (additionalValuesToMerge != null)
        {
            foreach (IParsable? additionalValue in additionalValuesToMerge)
            {
                additionalValue?.Serialize(this);
            }
        }

        this.writer.WriteEndObject();
    }

    /// <summary>
    /// Writes a collection of object values.
    /// </summary>
    /// <typeparam name="T">The type of objects in the collection.</typeparam>
    /// <param name="key">The property key.</param>
    /// <param name="values">The collection of values to write.</param>
    public void WriteCollectionOfObjectValues<T>(string? key, IEnumerable<T>? values)
        where T : IParsable
    {
        if (values == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        if (key != null)
        {
            this.writer.WriteStartArray(key);
        }
        else
        {
            this.writer.WriteStartArray();
        }

        foreach (T value in values)
        {
            this.WriteObjectValue(null, value);
        }

        this.writer.WriteEndArray();
    }

    /// <summary>
    /// Writes an enum value using the custom JSON serialization options.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="key">The property key.</param>
    /// <param name="value">The enum value to write.</param>
    public void WriteEnumValue<T>(string? key, T? value)
        where T : struct, Enum
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        // Use JsonSerializer with custom options to handle enum serialization
        string enumString = JsonSerializer.Serialize(value.Value, this.options).Trim('"');

        if (key != null)
        {
            this.writer.WriteString(key, enumString);
        }
        else
        {
            this.writer.WriteStringValue(enumString);
        }
    }

    /// <summary>
    /// Writes a boolean value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The boolean value to write.</param>
    public void WriteBoolValue(string? key, bool? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        if (key != null)
        {
            this.writer.WriteBoolean(key, value.Value);
        }
        else
        {
            this.writer.WriteBooleanValue(value.Value);
        }
    }

    /// <summary>
    /// Writes an integer value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The integer value to write.</param>
    public void WriteIntValue(string? key, int? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        if (key != null)
        {
            this.writer.WriteNumber(key, value.Value);
        }
        else
        {
            this.writer.WriteNumberValue(value.Value);
        }
    }

    /// <summary>
    /// Writes a double value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The double value to write.</param>
    public void WriteDoubleValue(string? key, double? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        if (key != null)
        {
            this.writer.WriteNumber(key, value.Value);
        }
        else
        {
            this.writer.WriteNumberValue(value.Value);
        }
    }

    /// <summary>
    /// Writes a decimal value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The decimal value to write.</param>
    public void WriteDecimalValue(string? key, decimal? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        if (key != null)
        {
            this.writer.WriteNumber(key, value.Value);
        }
        else
        {
            this.writer.WriteNumberValue(value.Value);
        }
    }

    /// <summary>
    /// Writes a DateTime value using the custom JSON serialization options.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The DateTime value to write.</param>
    public void WriteDateTimeValue(string? key, DateTime? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        // Use JsonSerializer with custom options to handle DateTime serialization
        string dateTimeString = JsonSerializer.Serialize(value.Value, this.options).Trim('"');

        if (key != null)
        {
            this.writer.WriteString(key, dateTimeString);
        }
        else
        {
            this.writer.WriteStringValue(dateTimeString);
        }
    }

    /// <summary>
    /// Writes a DateTimeOffset value using the custom JSON serialization options.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The DateTimeOffset value to write.</param>
    public void WriteDateTimeOffsetValue(string? key, DateTimeOffset? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        // Use JsonSerializer with custom options to handle DateTimeOffset serialization
        string dateTimeOffsetString = JsonSerializer.Serialize(value.Value, this.options).Trim('"');

        if (key != null)
        {
            this.writer.WriteString(key, dateTimeOffsetString);
        }
        else
        {
            this.writer.WriteStringValue(dateTimeOffsetString);
        }
    }

    /// <summary>
    /// Writes a TimeSpan value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The TimeSpan value to write.</param>
    public void WriteTimeSpanValue(string? key, TimeSpan? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        string timeSpanString = value.Value.ToString();

        if (key != null)
        {
            this.writer.WriteString(key, timeSpanString);
        }
        else
        {
            this.writer.WriteStringValue(timeSpanString);
        }
    }

    /// <summary>
    /// Writes a DateOnly value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The DateOnly value to write.</param>
    public void WriteDateOnlyValue(string? key, DateOnly? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        string dateOnlyString = value.Value.ToString("yyyy-MM-dd");

        if (key != null)
        {
            this.writer.WriteString(key, dateOnlyString);
        }
        else
        {
            this.writer.WriteStringValue(dateOnlyString);
        }
    }

    /// <summary>
    /// Writes a TimeOnly value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The TimeOnly value to write.</param>
    public void WriteTimeOnlyValue(string? key, TimeOnly? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        string timeOnlyString = value.Value.ToString("HH:mm:ss.fffffff");

        if (key != null)
        {
            this.writer.WriteString(key, timeOnlyString);
        }
        else
        {
            this.writer.WriteStringValue(timeOnlyString);
        }
    }

    /// <summary>
    /// Writes a GUID value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The GUID value to write.</param>
    public void WriteGuidValue(string? key, Guid? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        string guidString = value.Value.ToString();

        if (key != null)
        {
            this.writer.WriteString(key, guidString);
        }
        else
        {
            this.writer.WriteStringValue(guidString);
        }
    }

    /// <summary>
    /// Writes a collection of primitive values.
    /// </summary>
    /// <typeparam name="T">The type of primitive values.</typeparam>
    /// <param name="key">The property key.</param>
    /// <param name="values">The collection of values to write.</param>
    public void WriteCollectionOfPrimitiveValues<T>(string? key, IEnumerable<T>? values)
    {
        if (values == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        if (key != null)
        {
            this.writer.WriteStartArray(key);
        }
        else
        {
            this.writer.WriteStartArray();
        }

        foreach (T? value in values)
        {
            // Use JsonSerializer with custom options for complex types
            if (value is Enum enumValue)
            {
                string enumString = JsonSerializer.Serialize(enumValue, this.options).Trim('"');
                this.writer.WriteStringValue(enumString);
            }
            else if (value is DateTime dateTimeValue)
            {
                string dateTimeString = JsonSerializer.Serialize(dateTimeValue, this.options).Trim('"');
                this.writer.WriteStringValue(dateTimeString);
            }
            else if (value is DateTimeOffset dateTimeOffsetValue)
            {
                string dateTimeOffsetString = JsonSerializer.Serialize(dateTimeOffsetValue, this.options).Trim('"');
                this.writer.WriteStringValue(dateTimeOffsetString);
            }
            else
            {
                // For simple primitives, use direct writer methods
                switch (value)
                {
                    case string stringValue:
                        this.writer.WriteStringValue(stringValue);
                        break;
                    case int intValue:
                        this.writer.WriteNumberValue(intValue);
                        break;
                    case double doubleValue:
                        this.writer.WriteNumberValue(doubleValue);
                        break;
                    case decimal decimalValue:
                        this.writer.WriteNumberValue(decimalValue);
                        break;
                    case bool boolValue:
                        this.writer.WriteBooleanValue(boolValue);
                        break;
                    case Guid guidValue:
                        this.writer.WriteStringValue(guidValue.ToString());
                        break;
                    default:
                        // Fallback to JsonSerializer for other types
                        string jsonString = JsonSerializer.Serialize(value, this.options);
                        this.writer.WriteRawValue(jsonString);
                        break;
                }
            }
        }

        this.writer.WriteEndArray();
    }

    /// <summary>
    /// Writes a byte array value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The byte array to write.</param>
    public void WriteByteArrayValue(string? key, byte[]? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        string base64String = Convert.ToBase64String(value);

        if (key != null)
        {
            this.writer.WriteString(key, base64String);
        }
        else
        {
            this.writer.WriteStringValue(base64String);
        }
    }

    /// <summary>
    /// Writes additional data properties.
    /// </summary>
    /// <param name="value">The additional data dictionary.</param>
    public void WriteAdditionalData(IDictionary<string, object> value)
    {
        if (value == null)
        {
            return;
        }

        foreach (KeyValuePair<string, object> kvp in value)
        {
            // Use JsonSerializer with custom options for the value
            string jsonString = JsonSerializer.Serialize(kvp.Value, this.options);
            this.writer.WritePropertyName(kvp.Key);
            this.writer.WriteRawValue(jsonString);
        }
    }

    /// <summary>
    /// Writes a byte value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The byte value to write.</param>
    public void WriteByteValue(string? key, byte? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        if (key != null)
        {
            this.writer.WriteNumber(key, value.Value);
        }
        else
        {
            this.writer.WriteNumberValue(value.Value);
        }
    }

    /// <summary>
    /// Writes a signed byte value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The signed byte value to write.</param>
    public void WriteSbyteValue(string? key, sbyte? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        if (key != null)
        {
            this.writer.WriteNumber(key, value.Value);
        }
        else
        {
            this.writer.WriteNumberValue(value.Value);
        }
    }

    /// <summary>
    /// Writes a float value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The float value to write.</param>
    public void WriteFloatValue(string? key, float? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        if (key != null)
        {
            this.writer.WriteNumber(key, value.Value);
        }
        else
        {
            this.writer.WriteNumberValue(value.Value);
        }
    }

    /// <summary>
    /// Writes a long value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The long value to write.</param>
    public void WriteLongValue(string? key, long? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        if (key != null)
        {
            this.writer.WriteNumber(key, value.Value);
        }
        else
        {
            this.writer.WriteNumberValue(value.Value);
        }
    }

    /// <summary>
    /// Writes a Date value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The Date value to write.</param>
    public void WriteDateValue(string? key, Date? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        string dateString = value.Value.ToString();

        if (key != null)
        {
            this.writer.WriteString(key, dateString);
        }
        else
        {
            this.writer.WriteStringValue(dateString);
        }
    }

    /// <summary>
    /// Writes a Time value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The Time value to write.</param>
    public void WriteTimeValue(string? key, Time? value)
    {
        if (value == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        string timeString = value.Value.ToString();

        if (key != null)
        {
            this.writer.WriteString(key, timeString);
        }
        else
        {
            this.writer.WriteStringValue(timeString);
        }
    }

    /// <summary>
    /// Writes a collection of enum values.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="key">The property key.</param>
    /// <param name="values">The collection of enum values to write.</param>
    public void WriteCollectionOfEnumValues<T>(string? key, IEnumerable<T?>? values)
        where T : struct, Enum
    {
        if (values == null)
        {
            if (key != null)
            {
                this.writer.WriteNull(key);
            }
            else
            {
                this.writer.WriteNullValue();
            }

            return;
        }

        if (key != null)
        {
            this.writer.WriteStartArray(key);
        }
        else
        {
            this.writer.WriteStartArray();
        }

        foreach (T? value in values)
        {
            this.WriteEnumValue(null, value);
        }

        this.writer.WriteEndArray();
    }

    /// <summary>
    /// Writes a null value.
    /// </summary>
    /// <param name="key">The property key.</param>
    public void WriteNullValue(string? key)
    {
        if (key != null)
        {
            this.writer.WriteNull(key);
        }
        else
        {
            this.writer.WriteNullValue();
        }
    }

    /// <summary>
    /// Gets or sets the action called before the object gets serialized.
    /// </summary>
    public Action<IParsable>? OnBeforeObjectSerialization { get; set; }

    /// <summary>
    /// Gets or sets the action called after the object gets serialized.
    /// </summary>
    public Action<IParsable>? OnAfterObjectSerialization { get; set; }

    /// <summary>
    /// Gets or sets the action called when the serialization starts.
    /// </summary>
    public Action<IParsable, ISerializationWriter>? OnStartObjectSerialization { get; set; }

    /// <summary>
    /// Writes an UntypedNode value by recursively serializing its underlying value directly.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="untypedNode">The UntypedNode to write.</param>
    private void WriteUntypedNodeValue(string? key, UntypedNode untypedNode)
    {
        switch (untypedNode)
        {
            case UntypedInteger intNode:
                this.WriteIntValue(key, intNode.GetValue());
                break;

            case UntypedString stringNode:
                this.WriteStringValue(key, stringNode.GetValue());
                break;

            case UntypedBoolean boolNode:
                this.WriteBoolValue(key, boolNode.GetValue());
                break;

            case UntypedDouble doubleNode:
                this.WriteDoubleValue(key, doubleNode.GetValue());
                break;

            case UntypedDecimal decimalNode:
                this.WriteDecimalValue(key, decimalNode.GetValue());
                break;

            case UntypedObject objectNode:
                this.WriteUntypedObject(key, objectNode);
                break;

            case UntypedArray arrayNode:
                this.WriteUntypedArray(key, arrayNode);
                break;

            default:
                // Handle any additional UntypedNode types that might exist
                // by falling back to extracting the raw value and serializing it
                object? rawValue = untypedNode switch
                {
                    var node when node.GetType().Name.StartsWith("Untyped") => 
                        node.GetType().GetMethod("GetValue")?.Invoke(node, null),
                    _ => null
                };

                if (rawValue == null)
                {
                    this.WriteNullValue(key);
                }
                else
                {
                    // Use JsonSerializer for any unrecognized types
                    string json = JsonSerializer.Serialize(rawValue, this.options);
                    if (key != null)
                    {
                        this.writer.WritePropertyName(key);
                    }
                    this.writer.WriteRawValue(json);
                }
                break;
        }
    }

    /// <summary>
    /// Writes an UntypedObject by recursively writing its properties.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="untypedObject">The UntypedObject to write.</param>
    private void WriteUntypedObject(string? key, UntypedObject untypedObject)
    {
        IDictionary<string, UntypedNode>? objectData = untypedObject.GetValue();
        
        if (objectData == null)
        {
            this.WriteNullValue(key);
            return;
        }

        if (key != null)
        {
            this.writer.WriteStartObject(key);
        }
        else
        {
            this.writer.WriteStartObject();
        }

        foreach (KeyValuePair<string, UntypedNode> property in objectData)
        {
            this.WriteUntypedNodeValue(property.Key, property.Value);
        }

        this.writer.WriteEndObject();
    }

    /// <summary>
    /// Writes an UntypedArray by recursively writing its elements.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="untypedArray">The UntypedArray to write.</param>
    private void WriteUntypedArray(string? key, UntypedArray untypedArray)
    {
        IEnumerable<UntypedNode>? arrayData = untypedArray.GetValue();
        
        if (arrayData == null)
        {
            this.WriteNullValue(key);
            return;
        }

        if (key != null)
        {
            this.writer.WriteStartArray(key);
        }
        else
        {
            this.writer.WriteStartArray();
        }

        foreach (UntypedNode element in arrayData)
        {
            this.WriteUntypedNodeValue(null, element);
        }

        this.writer.WriteEndArray();
    }

    /// <summary>
    /// Disposes the writer and underlying resources.
    /// </summary>
    public void Dispose()
    {
        if (!this.disposed)
        {
            // The Stream doesn't get Disposed because it might still be in use.
            this.writer?.Dispose();
            this.disposed = true;
        }
    }
}