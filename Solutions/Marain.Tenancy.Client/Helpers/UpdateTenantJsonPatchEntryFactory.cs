namespace Marain.Tenancy.Client.Helpers;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Marain.Tenancy.Client.Models;
using Microsoft.Kiota.Abstractions.Serialization;

public static class UpdateTenantJsonPatchEntryFactory
{
    /// <summary>
    /// Creates an entry from an arbitrary object.
    /// </summary>
    /// <param name="operation">The <see cref="UpdateTenantJsonPatchEntry.Op" />.</param>
    /// <param name="path">The <see cref="UpdateTenantJsonPatchEntry.Path" />.</param>
    /// <param name="value">The <see cref="UpdateTenantJsonPatchEntry.Value" />.</param>
    public static UpdateTenantJsonPatchEntry Create(
        UpdateTenantJsonPatchEntryOperation operation,
        string path,
        object? value,
        JsonSerializerOptions serializerOptions)
    {
        if (value is null)
        {
            return new() { Op = operation, Path = path, Value = new UntypedNull() };
        }

        // Need to convert the object to something that will serialize correctly.
        string json = JsonSerializer.Serialize(value, serializerOptions);
        using var doc = JsonDocument.Parse(json);
        
        return new()
        {
            Op = operation,
            Path = path,
            Value = ConvertJsonElementToUntypedNode(doc.RootElement),
        };
    }

    private static void AddJsonPropertyToDictionary(JsonProperty property, IDictionary<string, UntypedNode> dictionary)
    {
        dictionary[property.Name] = ConvertJsonElementToUntypedNode(property.Value);
    }

    private static UntypedNode ConvertJsonElementToUntypedNode(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonElementToUntypedObject(element),
            JsonValueKind.Array => ConvertJsonElementToUntypedArray(element),
            JsonValueKind.String => new UntypedString(element.GetString()),
            JsonValueKind.Null => new UntypedNull(),
            JsonValueKind.Undefined => new UntypedNull(),
            JsonValueKind.True => new UntypedBoolean(true),
            JsonValueKind.False => new UntypedBoolean(false),
            JsonValueKind.Number => ConvertJsonElementToUntypedNumericNode(element),
            _ => new UntypedNode(),
        };
    }

    private static UntypedObject ConvertJsonElementToUntypedObject(JsonElement element)
    {
        Dictionary<string, UntypedNode> dict = [];

        foreach (JsonProperty property in element.EnumerateObject())
        {
            AddJsonPropertyToDictionary(property, dict);
        }

        return new UntypedObject(dict);
    }

    private static UntypedArray ConvertJsonElementToUntypedArray(JsonElement arrayElement)
    {
        if (arrayElement.ValueKind != JsonValueKind.Array)
        {
            throw new ArgumentException("JsonElement must be an array", nameof(arrayElement));
        }

        List<UntypedNode> list = new(arrayElement.GetArrayLength());

        foreach (JsonElement element in arrayElement.EnumerateArray())
        {
            list.Add(ConvertJsonElementToUntypedNode(element));
        }

        return new UntypedArray(list);
    }

    private static UntypedNode ConvertJsonElementToUntypedNumericNode(JsonElement numberElement)
    {
        if (numberElement.ValueKind != JsonValueKind.Number)
        {
            throw new ArgumentException("JsonElement must be a number", nameof(numberElement));
        }

        // Try to get the raw text first to preserve exact representation
        string? rawText = numberElement.GetRawText();

        // Check if it contains a decimal point or scientific notation
        bool hasDecimal = rawText.Contains('.') || rawText.Contains('e') || rawText.Contains('E');

        if (!hasDecimal)
        {
            // Integer types - try in order from smallest to largest. Don't care about int16 because there
            // isn't a corresponding UntypedNode.
            // Unsigned integers larger than int.MaxValue will be converted to UntypedLong nodes.
            if (numberElement.TryGetInt32(out int int32Value))
            {
                return new UntypedInteger(int32Value);
            }
            if (numberElement.TryGetInt64(out long int64Value))
            {
                return new UntypedLong(int64Value);
            }

            // Fallback to decimal for very large integers - e.g. unsigned long values larger than long.MaxValue.
            if (numberElement.TryGetDecimal(out decimal decimalValue))
            {
                return new UntypedDecimal(decimalValue);
            }
        }
        else
        {
            // Decimal types - try in order of precision
            if (numberElement.TryGetSingle(out float floatValue) &&
                float.IsFinite(floatValue) &&
                rawText == floatValue.ToString("G9", CultureInfo.InvariantCulture))
            {
                return new UntypedFloat(floatValue);
            }

            if (numberElement.TryGetDouble(out double doubleValue) &&
                double.IsFinite(doubleValue))
            {
                return new UntypedDouble(doubleValue);
            }

            if (numberElement.TryGetDecimal(out decimal decimalValue))
            {
                return new UntypedDecimal(decimalValue);
            }
        }

        // Ultimate fallback - return as string to preserve exact representation
        return new UntypedString(rawText);
    }

    /// <summary>
    /// Creates an entry from an integer.
    /// </summary>
    /// <param name="operation">The <see cref="UpdateTenantJsonPatchEntry.Op" />.</param>
    /// <param name="path">The <see cref="UpdateTenantJsonPatchEntry.Path" />.</param>
    /// <param name="value">The <see cref="UpdateTenantJsonPatchEntry.Value" />.</param>
    public static UpdateTenantJsonPatchEntry Create(
        UpdateTenantJsonPatchEntryOperation operation,
        string path,
        int value)
    {
        return new()
        {
            Op = operation,
            Path = path,
            Value = new UntypedInteger(value),
        };
    }

    /// <summary>
    /// Creates an entry from a string.
    /// </summary>
    /// <param name="operation">The <see cref="UpdateTenantJsonPatchEntry.Op" />.</param>
    /// <param name="path">The <see cref="UpdateTenantJsonPatchEntry.Path" />.</param>
    /// <param name="value">The <see cref="UpdateTenantJsonPatchEntry.Value" />.</param>
    public static UpdateTenantJsonPatchEntry Create(
        UpdateTenantJsonPatchEntryOperation operation,
        string path,
        string value)
    {
        return new()
        {
            Op = operation,
            Path = path,
            Value = new UntypedString(value),
        };
    }

    /// <summary>
    /// Creates an delete entry.
    /// </summary>
    /// <param name="path">The <see cref="UpdateTenantJsonPatchEntry.Path" />.</param>
    public static UpdateTenantJsonPatchEntry CreateDeleteEntry(string path)
    {
        return new()
        {
            Op = UpdateTenantJsonPatchEntryOperation.Remove,
            Path = path,
        };
    }
}
