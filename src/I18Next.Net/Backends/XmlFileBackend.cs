using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Loads translations from XML files, <c>{basePath}/{language}/{namespace}.xml</c>, using the element names as keys.
///     Below the root element, elements with child elements become groups and the text of the other elements becomes
///     the translation, e.g. <c>&lt;menu&gt;&lt;open&gt;Open&lt;/open&gt;&lt;/menu&gt;</c> for the key <c>menu.open</c>.
///     When the file of a regional language like <c>de-CH</c> does not exist, the file of its language part
///     (<c>de</c>) is loaded.
/// </summary>
/// <param name="basePath">The directory containing a subdirectory per language.</param>
/// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
/// <seealso cref="StrictXmlFileBackend" />
public class XmlFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory) : ITranslationBackend
{
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory = treeBuilderFactory;

    /// <summary>
    ///     Creates a backend loading XML files from the given directory into hierarchical translation trees.
    /// </summary>
    /// <param name="basePath">The directory containing a subdirectory per language.</param>
    public XmlFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    /// <summary>
    ///     Creates a backend loading XML files from the <c>locales</c> directory with the given tree builders.
    /// </summary>
    /// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
    public XmlFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("locales", treeBuilderFactory)
    {
    }

    /// <summary>
    ///     Creates a backend loading XML files from the <c>locales</c> directory into hierarchical translation trees.
    /// </summary>
    public XmlFileBackend()
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
            var key = path + childNode.Name;

            if (childNode.HasElements)
                PopulateTreeBuilder(key, childNode, builder);
            else
                builder.AddTranslation(key, childNode.Value);
        }
    }
}
