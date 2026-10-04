using System.IO;

using I18Next.Net.TranslationTrees;

using YamlDotNet.RepresentationModel;

namespace I18Next.Net.Yaml;

/// <summary>
///     Reads YAML translation resources into translation trees. Mappings form the key groups and sequences are stored as
///     arrays (<c>key.0</c>).
/// </summary>
public static class YamlTranslationReader
{
    public static ITranslationTree Read(string yaml, string @namespace = null)
    {
        var builder = new HierarchicalTranslationTreeBuilder { Namespace = @namespace };

        return Read(yaml, builder);
    }

    public static ITranslationTree Read(string yaml, ITranslationTreeBuilder builder)
    {
        using var reader = new StringReader(yaml);

        return Read(reader, builder);
    }

    public static ITranslationTree Read(TextReader reader, ITranslationTreeBuilder builder)
    {
        var stream = new YamlStream();
        stream.Load(reader);

        if (stream.Documents.Count > 0 && stream.Documents[0].RootNode is YamlMappingNode root)
            AddMapping("", root, builder);

        return builder.Build();
    }

    private static void AddMapping(string path, YamlMappingNode mapping, ITranslationTreeBuilder builder)
    {
        foreach (var entry in mapping.Children)
        {
            if (entry.Key is YamlScalarNode { Value: { } name })
                AddValue(path.Length == 0 ? name : path + "." + name, entry.Value, builder);
        }
    }

    private static void AddValue(string key, YamlNode node, ITranslationTreeBuilder builder)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                AddMapping(key, mapping, builder);
                break;
            case YamlSequenceNode sequence:
                for (var i = 0; i < sequence.Children.Count; i++)
                    AddValue($"{key}.{i}", sequence.Children[i], builder);

                break;
            case YamlScalarNode scalar when !IsNull(scalar):
                builder.AddTranslation(key, scalar.Value);
                break;
        }
    }

    private static bool IsNull(YamlScalarNode scalar)
    {
        return scalar.Value == null
               || (scalar.Style == YamlDotNet.Core.ScalarStyle.Plain && scalar.Value is "" or "~" or "null" or "Null" or "NULL");
    }
}
