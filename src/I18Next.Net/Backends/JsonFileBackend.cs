using System.IO;
using System.Text;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Loads translations from JSON files in the i18next format, <c>{basePath}/{language}/{namespace}.json</c>. Nested
///     objects become groups like <c>menu.file.open</c>, arrays are stored with their indexes as keys like <c>items.0</c>,
///     and comments and trailing commas are allowed.
///     When the file of a regional language like <c>de-CH</c> does not exist, the file of its language part
///     (<c>de</c>) is loaded.
/// </summary>
/// <param name="basePath">The directory containing a subdirectory per language.</param>
/// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
public class JsonFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory) : ITranslationBackend
{
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory = treeBuilderFactory;

    /// <summary>
    ///     Creates a backend loading JSON files from the given directory into hierarchical translation trees.
    /// </summary>
    /// <param name="basePath">The directory containing a subdirectory per language.</param>
    public JsonFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    /// <summary>
    ///     Creates a backend loading JSON files from the <c>locales</c> directory with the given tree builders.
    /// </summary>
    /// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
    public JsonFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("locales", treeBuilderFactory)
    {
    }

    /// <summary>
    ///     Creates a backend loading JSON files from the <c>locales</c> directory into hierarchical translation trees.
    /// </summary>
    public JsonFileBackend()
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
    public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        var path = FindFile(language, @namespace);

        if (path == null)
            return null;

        var builder = _treeBuilderFactory.Create();
        builder.Namespace = @namespace;

        if (Encoding.CodePage == Encoding.UTF8.CodePage)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

            return await JsonTranslationReader.ReadAsync(stream, builder).ConfigureAwait(false);
        }

        string content;

        using (var streamReader = new StreamReader(path, Encoding))
            content = await streamReader.ReadToEndAsync().ConfigureAwait(false);

        return JsonTranslationReader.Read(content, builder);
    }

    /// <summary>
    ///     Finds the file of a namespace. Override it to use another file layout.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>
    ///     <c>{basePath}/{language}/{namespace}.json</c>, else the file of the language part, or null if neither exists.
    /// </returns>
    protected virtual string FindFile(string language, string @namespace)
    {
        var path = Path.Combine(BasePath, language, @namespace + ".json");

        if (File.Exists(path))
            return path;

        path = Path.Combine(BasePath, BackendUtilities.GetLanguagePart(language), @namespace + ".json");

        return !File.Exists(path) ? null : path;
    }
}
