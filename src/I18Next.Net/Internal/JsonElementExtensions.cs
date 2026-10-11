using System.Collections.Generic;
using System.Text.Json;

namespace I18Next.Net.Internal;

/// <summary>
///     Converts <see cref="JsonElement" /> values to plain .NET objects, e.g. the arguments of nested translations.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    ///     Converts a JSON value to a plain .NET object: objects to <see cref="Dictionary{TKey,TValue}" /> of string and
    ///     object, arrays to object arrays, strings to <see cref="string" />, integral numbers to <see cref="long" />, other
    ///     numbers to <see cref="double" />, booleans to <see cref="bool" /> and null or undefined to null.
    /// </summary>
    /// <param name="element">The JSON value.</param>
    /// <returns>The converted value.</returns>
    public static object ToObject(this JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                return element.ToDictionary();
            case JsonValueKind.Array:
                var list = new List<object>(element.GetArrayLength());

                foreach (var item in element.EnumerateArray())
                    list.Add(item.ToObject());

                return list.ToArray();
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var longValue))
                    return longValue;

                return element.GetDouble();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            default:
                return null;
        }
    }

    /// <summary>
    ///     Converts the properties of a JSON object to a dictionary, converting the values like <see cref="ToObject" />.
    /// </summary>
    /// <param name="element">The JSON object.</param>
    /// <returns>The properties keyed by their names.</returns>
    /// <exception cref="System.InvalidOperationException">The element is not a JSON object.</exception>
    public static Dictionary<string, object> ToDictionary(this JsonElement element)
    {
        var result = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
            result[property.Name] = property.Value.ToObject();

        return result;
    }
}
