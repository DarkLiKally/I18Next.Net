using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace I18Next.Net.Tool.Resources;

/// <summary>
///     The resources of one language and namespace as an ordered JSON tree. Keys are joined with the key separator, an
///     empty separator means flat keys.
/// </summary>
internal sealed class TranslationDocument(JsonObject root, string keySeparator)
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public TranslationDocument(string keySeparator)
        : this([], keySeparator)
    {
    }

    public JsonObject Root { get; } = root;

    public string KeySeparator { get; } = keySeparator ?? string.Empty;

    public static TranslationDocument Parse(string json, string keySeparator)
    {
        JsonNode node;

        try
        {
            node = string.IsNullOrWhiteSpace(json) ? new JsonObject() : JsonNode.Parse(json, documentOptions: DocumentOptions);
            Materialize(node);
        }
        catch (Exception e) when (e is JsonException or ArgumentException)
        {
            throw new FormatException(e.Message, e);
        }

        return node is JsonObject root ? new TranslationDocument(root, keySeparator) : throw new FormatException("The root of the JSON is not an object.");
    }

    /// <summary>
    ///     Creates a document from flat keys, nesting them at the key separator.
    /// </summary>
    public static TranslationDocument FromEntries(IEnumerable<KeyValuePair<string, string>> entries, string keySeparator)
    {
        var document = new TranslationDocument(keySeparator);

        foreach (var entry in entries)
            document.SetValue(entry.Key, entry.Value);

        return document;
    }

    /// <summary>
    ///     Gets all values with their full keys. Array items get their index as key like at runtime.
    /// </summary>
    public IEnumerable<KeyValuePair<string, string>> GetEntries()
    {
        return GetEntries(Root, null, KeySeparator.Length == 0 ? "." : KeySeparator);
    }

    public bool ContainsKey(string key)
    {
        return Find(Root, key) != null;
    }

    public string GetValue(string key)
    {
        return Find(Root, key) is JsonValue value ? GetText(value) : null;
    }

    /// <summary>
    ///     Sets a value, creating the objects on the way. A key whose path is blocked by a value is stored as flat key in the
    ///     deepest object.
    /// </summary>
    public bool SetValue(string key, string value)
    {
        JsonNode node = Root;
        var remaining = key;

        while (true)
        {
            if (node is JsonArray array)
            {
                var (index, rest) = SplitIndex(array, remaining);

                if (index < 0)
                    return false;

                if (rest == null)
                {
                    array[index] = value;
                    return true;
                }

                if (array[index] is not JsonObject and not JsonArray)
                    return false;

                node = array[index];
                remaining = rest;
                continue;
            }

            var obj = (JsonObject)node;
            var separatorIndex = KeySeparator.Length == 0 ? -1 : remaining.IndexOf(KeySeparator, StringComparison.Ordinal);

            if (separatorIndex <= 0 || obj.ContainsKey(remaining))
            {
                obj[remaining] = value;
                return true;
            }

            var head = remaining.Substring(0, separatorIndex);
            var tail = remaining.Substring(separatorIndex + KeySeparator.Length);

            if (!obj.TryGetPropertyValue(head, out var child) || child == null)
            {
                child = new JsonObject();
                obj[head] = child;
            }
            else if (child is not JsonObject and not JsonArray)
            {
                obj[remaining] = value;
                return true;
            }

            node = child;
            remaining = tail;
        }
    }

    public bool Remove(string key)
    {
        return Remove(Root, key);
    }

    /// <summary>
    ///     Sorts the keys of all objects ordinally. Arrays keep their order.
    /// </summary>
    public void Sort()
    {
        Sort(Root);
    }

    public string ToJson()
    {
        return Root.ToJsonString(WriteOptions).Replace("\r\n", "\n") + "\n";
    }

    internal static string GetText(JsonValue value)
    {
        if (value.TryGetValue<string>(out var text))
            return text;

        var json = value.ToJsonString();

        return json switch
        {
            "true" => bool.TrueString,
            "false" => bool.FalseString,
            _ => json
        };
    }

    private static void Materialize(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj)
                    Materialize(property.Value);

                break;
            case JsonArray array:
                foreach (var item in array)
                    Materialize(item);

                break;
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> GetEntries(JsonNode node, string path, string separator)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj)
                {
                    foreach (var entry in GetEntries(property.Value, path == null ? property.Key : path + separator + property.Key, separator))
                        yield return entry;
                }

                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    foreach (var entry in GetEntries(array[i], path + separator + i.ToString(CultureInfo.InvariantCulture), separator))
                        yield return entry;
                }

                break;
            case JsonValue value:
                yield return new KeyValuePair<string, string>(path, GetText(value));
                break;
        }
    }

    private static void Sort(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                var properties = obj.OrderBy(p => p.Key, StringComparer.Ordinal).ToList();
                obj.Clear();

                foreach (var property in properties)
                {
                    Sort(property.Value);
                    obj.Add(property.Key, property.Value);
                }

                break;
            case JsonArray array:
                foreach (var item in array)
                    Sort(item);

                break;
        }
    }

    private (int Index, string Remainder) SplitIndex(JsonArray array, string key)
    {
        var separatorIndex = KeySeparator.Length == 0 ? -1 : key.IndexOf(KeySeparator, StringComparison.Ordinal);
        var head = separatorIndex < 0 ? key : key.Substring(0, separatorIndex);

        if (!int.TryParse(head, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index >= array.Count)
            return (-1, null);

        return (index, separatorIndex < 0 ? null : key.Substring(separatorIndex + KeySeparator.Length));
    }

    private JsonNode Find(JsonNode node, string key)
    {
        switch (node)
        {
            case JsonArray array:
                var (index, rest) = SplitIndex(array, key);

                return index < 0 ? null : rest == null ? array[index] : Find(array[index], rest);
            case JsonObject obj:
                if (obj.TryGetPropertyValue(key, out var value))
                    return value;

                foreach (var (head, tail) in GetSplits(key))
                {
                    if (obj.TryGetPropertyValue(head, out var child) && child is JsonObject or JsonArray && Find(child, tail) is { } found)
                        return found;
                }

                return null;
            default:
                return null;
        }
    }

    private bool Remove(JsonNode node, string key)
    {
        switch (node)
        {
            case JsonObject obj:
                if (obj.Remove(key))
                    return true;

                foreach (var (head, tail) in GetSplits(key))
                {
                    if (!obj.TryGetPropertyValue(head, out var child) || child is not JsonObject and not JsonArray || !Remove(child, tail))
                        continue;

                    if (child is JsonObject { Count: 0 })
                        obj.Remove(head);

                    return true;
                }

                return false;
            case JsonArray array:
                var (index, rest) = SplitIndex(array, key);

                return index >= 0 && rest != null && Remove(array[index], rest);
            default:
                return false;
        }
    }

    private IEnumerable<(string Head, string Tail)> GetSplits(string key)
    {
        if (KeySeparator.Length == 0)
            yield break;

        var index = key.IndexOf(KeySeparator, StringComparison.Ordinal);

        while (index > 0)
        {
            yield return (key.Substring(0, index), key.Substring(index + KeySeparator.Length));

            index = key.IndexOf(KeySeparator, index + KeySeparator.Length, StringComparison.Ordinal);
        }
    }
}
