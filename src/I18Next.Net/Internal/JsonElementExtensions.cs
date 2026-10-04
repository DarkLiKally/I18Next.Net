using System.Collections.Generic;
using System.Text.Json;

namespace I18Next.Net.Internal;

public static class JsonElementExtensions
{
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

    public static Dictionary<string, object> ToDictionary(this JsonElement element)
    {
        var result = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
            result[property.Name] = property.Value.ToObject();

        return result;
    }
}
