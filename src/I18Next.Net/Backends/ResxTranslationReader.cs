using System.IO;
using System.Xml;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Reads the string resources of .resx files. Other resources like images are skipped.
/// </summary>
public static class ResxTranslationReader
{
    private static readonly XmlReaderSettings Settings = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreComments = true };

    public static ITranslationTree Read(string content, ITranslationTreeBuilder builder)
    {
        using var reader = XmlReader.Create(new StringReader(content), Settings);

        return Read(reader, builder);
    }

    public static ITranslationTree Read(Stream stream, ITranslationTreeBuilder builder)
    {
        using var reader = XmlReader.Create(stream, Settings);

        return Read(reader, builder);
    }

    public static ITranslationTree Read(string content, string @namespace)
    {
        var builder = new HierarchicalTranslationTreeBuilder { Namespace = @namespace };

        return Read(content, builder);
    }

    private static ITranslationTree Read(XmlReader reader, ITranslationTreeBuilder builder)
    {
        while (reader.ReadToFollowing("data"))
        {
            var name = reader.GetAttribute("name");

            if (string.IsNullOrEmpty(name) || reader.GetAttribute("type") != null || reader.GetAttribute("mimetype") != null)
                continue;

            using var data = reader.ReadSubtree();

            if (data.ReadToFollowing("value"))
                builder.AddTranslation(name, data.ReadElementContentAsString());
        }

        return builder.Build();
    }
}
