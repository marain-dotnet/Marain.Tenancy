// <copyright file="CorvusJsonParseNode.cs" company="Endjin Limited">
// Copyright (c) Endjin Limited. All rights reserved.
// </copyright>

namespace Marain.Tenancy.Client.Serialization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;

/// <summary>
/// A custom JSON parse node that uses Corvus JSON serialization options.
/// </summary>
public class CorvusJsonParseNode : IParseNode
{
    private readonly JsonElement _jsonElement;
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorvusJsonParseNode"/> class.
    /// </summary>
    /// <param name="jsonElement">The JSON element to parse.</param>
    /// <param name="options">The JSON serializer options.</param>
    public CorvusJsonParseNode(JsonElement jsonElement, JsonSerializerOptions options)
    {
        this._jsonElement = jsonElement;
        this._options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets an object value using the specified factory.
    /// </summary>
    /// <typeparam name="T">The type of object to create.</typeparam>
    /// <param name="factory">The factory method to create the object.</param>
    /// <returns>The deserialized object.</returns>
    public T GetObjectValue<T>(ParsableFactory<T> factory)
        where T : IParsable
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return default!;
        }

        // Create the object instance
        T instance = factory(this);
        
        // Deserialize the object by calling its field deserializers
        if (this._jsonElement.ValueKind == JsonValueKind.Object && instance != null)
        {
            IDictionary<string, Action<IParseNode>> fieldDeserializers = instance.GetFieldDeserializers();
            
            // Call OnBeforeAssignFieldValues if available
            this.OnBeforeAssignFieldValues?.Invoke(instance);
            
            // Process each field in the JSON object
            foreach (JsonProperty property in this._jsonElement.EnumerateObject())
            {
                if (fieldDeserializers.TryGetValue(property.Name, out Action<IParseNode>? deserializer))
                {
                    var childNode = new CorvusJsonParseNode(property.Value, this._options);
                    deserializer(childNode);
                }
            }
            
            // Call OnAfterAssignFieldValues if available
            this.OnAfterAssignFieldValues?.Invoke(instance);
        }

        return instance!;
    }

    /// <summary>
    /// Gets a string value from the current node.
    /// </summary>
    /// <returns>The string value or null.</returns>
    public string? GetStringValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return this._jsonElement.GetString();
    }

    /// <summary>
    /// Gets an enum value using the custom JSON serialization options.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <returns>The enum value or null.</returns>
    public T? GetEnumValue<T>()
        where T : struct, Enum
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        // Use JsonSerializer with custom options to handle enum deserialization
        string jsonString = this._jsonElement.GetRawText();
        return JsonSerializer.Deserialize<T>(jsonString, this._options);
    }

    /// <summary>
    /// Gets a boolean value from the current node.
    /// </summary>
    /// <returns>The boolean value or null.</returns>
    public bool? GetBoolValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return this._jsonElement.GetBoolean();
    }

    /// <summary>
    /// Gets an integer value from the current node.
    /// </summary>
    /// <returns>The integer value or null.</returns>
    public int? GetIntValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return this._jsonElement.GetInt32();
    }

    /// <summary>
    /// Gets a double value from the current node.
    /// </summary>
    /// <returns>The double value or null.</returns>
    public double? GetDoubleValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return this._jsonElement.GetDouble();
    }

    /// <summary>
    /// Gets a decimal value from the current node.
    /// </summary>
    /// <returns>The decimal value or null.</returns>
    public decimal? GetDecimalValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return this._jsonElement.GetDecimal();
    }

    /// <summary>
    /// Gets a DateTime value using the custom JSON serialization options.
    /// </summary>
    /// <returns>The DateTime value or null.</returns>
    public DateTime? GetDateTimeValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        // Use JsonSerializer with custom options to handle DateTime deserialization
        string jsonString = this._jsonElement.GetRawText();
        return JsonSerializer.Deserialize<DateTime>(jsonString, this._options);
    }

    /// <summary>
    /// Gets a DateTimeOffset value using the custom JSON serialization options.
    /// </summary>
    /// <returns>The DateTimeOffset value or null.</returns>
    public DateTimeOffset? GetDateTimeOffsetValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        // Use JsonSerializer with custom options to handle DateTimeOffset deserialization
        string jsonString = this._jsonElement.GetRawText();
        return JsonSerializer.Deserialize<DateTimeOffset>(jsonString, this._options);
    }

    /// <summary>
    /// Gets a TimeSpan value from the current node.
    /// </summary>
    /// <returns>The TimeSpan value or null.</returns>
    public TimeSpan? GetTimeSpanValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        string? timeSpanString = this._jsonElement.GetString();
        return timeSpanString != null ? TimeSpan.Parse(timeSpanString) : null;
    }

    /// <summary>
    /// Gets a DateOnly value from the current node.
    /// </summary>
    /// <returns>The DateOnly value or null.</returns>
    public DateOnly? GetDateOnlyValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        string? dateString = this._jsonElement.GetString();
        return dateString != null ? DateOnly.ParseExact(dateString, "yyyy-MM-dd") : null;
    }

    /// <summary>
    /// Gets a TimeOnly value from the current node.
    /// </summary>
    /// <returns>The TimeOnly value or null.</returns>
    public TimeOnly? GetTimeOnlyValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        string? timeString = this._jsonElement.GetString();
        return timeString != null ? TimeOnly.ParseExact(timeString, "HH:mm:ss.fffffff") : null;
    }

    /// <summary>
    /// Gets a GUID value from the current node.
    /// </summary>
    /// <returns>The GUID value or null.</returns>
    public Guid? GetGuidValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        string? guidString = this._jsonElement.GetString();
        return guidString != null ? Guid.Parse(guidString) : null;
    }

    /// <summary>
    /// Gets a byte array value from the current node.
    /// </summary>
    /// <returns>The byte array or null.</returns>
    public byte[]? GetByteArrayValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        string? base64String = this._jsonElement.GetString();
        return base64String != null ? Convert.FromBase64String(base64String) : null;
    }

    /// <summary>
    /// Gets a collection of object values using the specified factory.
    /// </summary>
    /// <typeparam name="T">The type of objects in the collection.</typeparam>
    /// <param name="factory">The factory method to create objects.</param>
    /// <returns>A collection of deserialized objects.</returns>
    public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory)
        where T : IParsable
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (this._jsonElement.ValueKind != JsonValueKind.Array)
        {
            return Enumerable.Empty<T>();
        }

        var result = new List<T>();
        foreach (JsonElement element in this._jsonElement.EnumerateArray())
        {
            var parseNode = new CorvusJsonParseNode(element, this._options);
            T value = parseNode.GetObjectValue(factory);
            if (value != null)
            {
                result.Add(value);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets a collection of primitive values.
    /// </summary>
    /// <typeparam name="T">The type of primitive values.</typeparam>
    /// <returns>A collection of primitive values.</returns>
    public IEnumerable<T> GetCollectionOfPrimitiveValues<T>()
    {
        if (this._jsonElement.ValueKind != JsonValueKind.Array)
        {
            return Enumerable.Empty<T>();
        }

        var result = new List<T>();
        foreach (JsonElement element in this._jsonElement.EnumerateArray())
        {
            var parseNode = new CorvusJsonParseNode(element, this._options);

            var value = default(T);

            // Handle different primitive types
            if (typeof(T) == typeof(string))
            {
                value = (T?)(object?)parseNode.GetStringValue();
            }
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
            {
                value = (T?)(object?)parseNode.GetIntValue();
            }
            else if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
            {
                value = (T?)(object?)parseNode.GetDoubleValue();
            }
            else if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
            {
                value = (T?)(object?)parseNode.GetDecimalValue();
            }
            else if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                value = (T?)(object?)parseNode.GetBoolValue();
            }
            else if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                value = (T?)(object?)parseNode.GetDateTimeValue();
            }
            else if (typeof(T) == typeof(DateTimeOffset) || typeof(T) == typeof(DateTimeOffset?))
            {
                value = (T?)(object?)parseNode.GetDateTimeOffsetValue();
            }
            else if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
            {
                value = (T?)(object?)parseNode.GetGuidValue();
            }
            else if (typeof(T).IsEnum)
            {
                // Use JsonSerializer with custom options for enum types
                string jsonString = element.GetRawText();
                value = JsonSerializer.Deserialize<T>(jsonString, this._options);
            }
            else
            {
                // Fallback: use JsonSerializer for other types
                string jsonString = element.GetRawText();
                value = JsonSerializer.Deserialize<T>(jsonString, this._options);
            }

            if (value != null)
            {
                result.Add(value);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets a collection of enum values.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <returns>A collection of enum values.</returns>
    public IEnumerable<T?> GetCollectionOfEnumValues<T>()
        where T : struct, Enum
    {
        if (this._jsonElement.ValueKind != JsonValueKind.Array)
        {
            return Enumerable.Empty<T?>();
        }

        var result = new List<T?>();
        foreach (JsonElement element in this._jsonElement.EnumerateArray())
        {
            var parseNode = new CorvusJsonParseNode(element, this._options);
            T? value = parseNode.GetEnumValue<T>();
            result.Add(value);
        }

        return result;
    }

    /// <summary>
    /// Gets a child node by field name.
    /// </summary>
    /// <param name="identifier">The field name.</param>
    /// <returns>A child parse node or null if not found.</returns>
    public IParseNode? GetChildNode(string identifier)
    {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        if (this._jsonElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (this._jsonElement.TryGetProperty(identifier, out JsonElement property))
        {
            return new CorvusJsonParseNode(property, this._options);
        }

        return null;
    }

    /// <summary>
    /// Gets a byte value from the current node.
    /// </summary>
    /// <returns>The byte value or null.</returns>
    public byte? GetByteValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return this._jsonElement.GetByte();
    }

    /// <summary>
    /// Gets a signed byte value from the current node.
    /// </summary>
    /// <returns>The signed byte value or null.</returns>
    public sbyte? GetSbyteValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return this._jsonElement.GetSByte();
    }

    /// <summary>
    /// Gets a float value from the current node.
    /// </summary>
    /// <returns>The float value or null.</returns>
    public float? GetFloatValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return this._jsonElement.GetSingle();
    }

    /// <summary>
    /// Gets a long value from the current node.
    /// </summary>
    /// <returns>The long value or null.</returns>
    public long? GetLongValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return this._jsonElement.GetInt64();
    }

    /// <summary>
    /// Gets a Date value from the current node.
    /// </summary>
    /// <returns>The Date value or null.</returns>
    public Date? GetDateValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        string? dateString = this._jsonElement.GetString();
        return dateString != null ? new Date(DateTime.Parse(dateString)) : null;
    }

    /// <summary>
    /// Gets a Time value from the current node.
    /// </summary>
    /// <returns>The Time value or null.</returns>
    public Time? GetTimeValue()
    {
        if (this._jsonElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        string? timeString = this._jsonElement.GetString();
        if (timeString != null && TimeOnly.TryParse(timeString, out TimeOnly timeOnly))
        {
            return new Time(new DateTime().Add(timeOnly.ToTimeSpan()));
        }
        return null;
    }

    /// <summary>
    /// Gets or sets the action called before the field values are assigned from the response.
    /// </summary>
    public Action<IParsable>? OnBeforeAssignFieldValues { get; set; }

    /// <summary>
    /// Gets or sets the action called after the field values are assigned from the response.
    /// </summary>
    public Action<IParsable>? OnAfterAssignFieldValues { get; set; }
}