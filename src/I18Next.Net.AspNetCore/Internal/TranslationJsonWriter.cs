#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace I18Next.Net.AspNetCore.Internal;

internal static class TranslationJsonWriter
{
    public static readonly JsonWriterOptions WriterOptions = new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    public static byte[] Write(IDictionary<string, string> values)
    {
        var root = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var entry in values)
            AddValue(root, entry.Key, entry.Value);

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, WriterOptions))
            WriteNode(writer, root);

        return stream.ToArray();
    }

    private static void AddValue(Dictionary<string, object> target, string key, string value)
    {
        var start = 0;
        int index;

        while ((index = key.IndexOf('.', start)) >= 0)
        {
            var part = key.Substring(start, index - start);

            if (!target.TryGetValue(part, out var child))
            {
                child = new Dictionary<string, object>(StringComparer.Ordinal);
                target.Add(part, child);
            }

            if (child is not Dictionary<string, object> group)
                break;

            target = group;
            start = index + 1;
        }

        var name = key.Substring(start);

        if (!target.ContainsKey(name))
            target.Add(name, value);
    }

    private static void WriteNode(Utf8JsonWriter writer, object node)
    {
        if (node is not Dictionary<string, object> group)
        {
            if (node is string value)
                writer.WriteStringValue(value);
            else
                writer.WriteNullValue();

            return;
        }

        if (IsArray(group))
        {
            writer.WriteStartArray();

            for (var i = 0; i < group.Count; i++)
                WriteNode(writer, group[i.ToString(CultureInfo.InvariantCulture)]);

            writer.WriteEndArray();

            return;
        }

        writer.WriteStartObject();

        foreach (var entry in group)
        {
            writer.WritePropertyName(entry.Key);
            WriteNode(writer, entry.Value);
        }

        writer.WriteEndObject();
    }

    private static bool IsArray(Dictionary<string, object> group)
    {
        if (group.Count == 0)
            return false;

        for (var i = 0; i < group.Count; i++)
        {
            if (!group.ContainsKey(i.ToString(CultureInfo.InvariantCulture)))
                return false;
        }

        return true;
    }
}
#endif
