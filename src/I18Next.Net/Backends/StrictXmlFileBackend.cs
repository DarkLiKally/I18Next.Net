using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Loads translations from XML files, <c>{basePath}/{language}/{namespace}.xml</c>, using <c>Section</c> elements
///     with a <c>name</c> attribute for groups and <c>Translation</c> elements with a <c>key</c> attribute for the
///     translations, e.g.
///     <c>&lt;Section name="menu"&gt;&lt;Translation key="open"&gt;Open&lt;/Translation&gt;&lt;/Section&gt;</c> for the key
///     <c>menu.open</c>. Allows keys which are no valid XML element names, unlike the
///     <see cref="XmlFileBackend" />. Other elements are ignored.
///     When the file of a regional language like <c>de-CH</c> does not exist, the file of its language part
///     (<c>de</c>) is loaded.
/// </summary>
/// <param name="basePath">The directory containing a subdirectory per language.</param>
/// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
public class StrictXmlFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory) : ITranslationBackend
{
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory = treeBuilderFactory;

    /// <summary>
    ///     Creates a backend loading XML files from the given directory into hierarchical translation trees.
    /// </summary>
    /// <param name="basePath">The directory containing a subdirectory per language.</param>
    public StrictXmlFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    /// <summary>
    ///     Creates a backend loading XML files from the <c>locales</c> directory with the given tree builders.
    /// </summary>
    /// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
    public StrictXmlFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("locales", treeBuilderFactory)
    {
    }

    /// <summary>
    ///     Creates a backend loading XML files from the <c>locales</c> directory into hierarchical translation trees.
    /// </summary>
    public StrictXmlFileBackend()
        : this("locales")
    {
    }

    /// <summary>
    ///     The directory containing a subdirectory per language. Relative paths are resolved against the current directory.
    /// </summary>
    protected string BasePath { get; } = basePath;

    /// <summary>
    ///     The encoding of the files. Defaults to UTF-8.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <inheritdoc />
    /// <remarks>Returns null if the file of the namespace does not exist.</remarks>
    public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        var path = FindFile(language, @namespace);

        if (path == null)
            return Task.FromResult<ITranslationTree>(null);

        XContainer parsedXml;

        using (var streamReader = new StreamReader(path, Encoding))
        {
            var document = XDocument.Load(streamReader);

            parsedXml = document.Root;
        }

        var builder = _treeBuilderFactory.Create();
        builder.Namespace = @namespace;

        PopulateTreeBuilder("", parsedXml, builder);

        return Task.FromResult(builder.Build());
    }

    /// <summary>
    ///     Finds the file of a namespace. Override it to use another file layout.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>
    ///     <c>{basePath}/{language}/{namespace}.xml</c>, else the file of the language part, or null if neither exists.
    /// </returns>
    protected virtual string FindFile(string language, string @namespace)
    {
        var path = Path.Combine(BasePath, language, @namespace + ".xml");

        if (File.Exists(path))
            return path;

        path = Path.Combine(BasePath, BackendUtilities.GetLanguagePart(language), @namespace + ".xml");

        return !File.Exists(path) ? null : path;
    }

    private static void PopulateTreeBuilder(string path, XContainer node, ITranslationTreeBuilder builder)
    {
        if (path != string.Empty)
            path += ".";

        foreach (var childNode in node.Elements())
        {
            switch (childNode.Name.LocalName)
            {
                case "Section":
                    if (childNode.HasElements)
                    {
                        var sectionAttribute = childNode.Attribute("name");

                        if (sectionAttribute != null)
                        {
                            var sectionKey = path + sectionAttribute.Value;

                            PopulateTreeBuilder(sectionKey, childNode, builder);
                        }
                    }

                    break;
                case "Translation":
                    var attribute = childNode.Attribute("key");

                    if (attribute != null)
                    {
                        var translationKey = path + attribute.Value;

                        builder.AddTranslation(translationKey, childNode.Value);
                    }

                    break;
            }
        }
    }
}
