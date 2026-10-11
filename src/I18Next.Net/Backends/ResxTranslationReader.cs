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

    /// <summary>
    ///     Reads the string resources of the given .resx content into the builder.
    /// </summary>
    /// <param name="content">The content of a .resx file.</param>
    /// <param name="builder">The builder the resources are added to, with the resource names as keys.</param>
    /// <returns>The built translation tree.</returns>
    /// <exception cref="System.Xml.XmlException">The content is no valid XML.</exception>
    public static ITranslationTree Read(string content, ITranslationTreeBuilder builder)
    {
        using var reader = XmlReader.Create(new StringReader(content), Settings);

        return Read(reader, builder);
    }

    /// <summary>
    ///     Reads the string resources of the given .resx stream into the builder.
    /// </summary>
    /// <param name="stream">The stream of a .resx file. It is not disposed.</param>
    /// <param name="builder">The builder the resources are added to, with the resource names as keys.</param>
    /// <returns>The built translation tree.</returns>
    /// <exception cref="System.Xml.XmlException">The content is no valid XML.</exception>
    public static ITranslationTree Read(Stream stream, ITranslationTreeBuilder builder)
    {
        using var reader = XmlReader.Create(stream, Settings);

        return Read(reader, builder);
    }

    /// <summary>
    ///     Reads the string resources of the given .resx content into a hierarchical translation tree, which splits the
    ///     resource names at dots into groups.
    /// </summary>
    /// <param name="content">The content of a .resx file.</param>
    /// <param name="namespace">The namespace of the translation tree.</param>
    /// <returns>The built translation tree.</returns>
    /// <exception cref="System.Xml.XmlException">The content is no valid XML.</exception>
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
