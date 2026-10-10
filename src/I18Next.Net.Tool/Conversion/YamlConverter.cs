using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

using I18Next.Net.Tool.Resources;

using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace I18Next.Net.Tool.Conversion;

/// <summary>
///     Converts YAML translation files from and to JSON trees like the YAML backend reads them.
/// </summary>
internal static class YamlConverter
{
    public static JsonObject Read(string yaml)
    {
        var stream = new YamlStream();

        try
        {
            using var reader = new StringReader(yaml);
            stream.Load(reader);
        }
        catch (YamlException e)
        {
            throw new FormatException(e.Message, e);
        }

        return stream.Documents.Count > 0 && stream.Documents[0].RootNode is YamlMappingNode root ? ReadMapping(root) : [];
    }

    public static string Write(JsonObject root)
    {
        var serializer = new SerializerBuilder().WithQuotingNecessaryStrings().Build();

        return serializer.Serialize(ToObject(root)).Replace("\r\n", "\n");
    }

    private static JsonObject ReadMapping(YamlMappingNode mapping)
    {
        var result = new JsonObject();

        foreach (var entry in mapping.Children)
        {
            if (entry.Key is YamlScalarNode { Value: { } name } && ReadNode(entry.Value) is { } value)
                result[name] = value;
        }

        return result;
    }

    private static JsonNode ReadNode(YamlNode node)
    {
        return node switch
        {
            YamlMappingNode mapping => ReadMapping(mapping),
            YamlSequenceNode sequence => new JsonArray(sequence.Children.Select(ReadNode).ToArray()),
            YamlScalarNode scalar when !IsNull(scalar) => JsonValue.Create(scalar.Value),
            _ => null
        };
    }

    private static bool IsNull(YamlScalarNode scalar)
    {
        return scalar.Value == null || (scalar.Style == ScalarStyle.Plain && scalar.Value is "" or "~" or "null" or "Null" or "NULL");
    }

    private static object ToObject(JsonNode node)
    {
        return node switch
        {
            JsonObject obj => obj.ToDictionary(p => p.Key, p => ToObject(p.Value)),
            JsonArray array => array.Select(ToObject).ToList(),
            JsonValue value => TranslationDocument.GetText(value),
            _ => null
        };
    }
}
