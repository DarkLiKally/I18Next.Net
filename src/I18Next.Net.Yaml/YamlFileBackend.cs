using System.IO;
using System.Text;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Yaml;

/// <summary>
///     Loads translations from <c>{basePath}/{language}/{namespace}.yaml</c> or <c>.yml</c> files, falling back to the
///     folder of the language part (e.g. <c>de</c> for <c>de-AT</c>).
/// </summary>
/// <param name="basePath">The folder containing the language folders.</param>
/// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
public class YamlFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory) : ITranslationBackend
{
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory = treeBuilderFactory;

    /// <summary>
    ///     Creates a backend loading the files from the given folder into hierarchical translation trees.
    /// </summary>
    /// <param name="basePath">The folder containing the language folders.</param>
    public YamlFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    /// <summary>
    ///     Creates a backend loading the files from the <c>locales</c> folder.
    /// </summary>
    /// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
    public YamlFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("locales", treeBuilderFactory)
    {
    }

    /// <summary>
    ///     Creates a backend loading the files from the <c>locales</c> folder into hierarchical translation trees.
    /// </summary>
    public YamlFileBackend()
        : this("locales")
    {
    }

    /// <summary>
    ///     The folder containing the language folders.
    /// </summary>
    protected string BasePath { get; } = basePath;

    /// <summary>
    ///     The encoding of the files, UTF-8 by default.
    /// </summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <inheritdoc />
    public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        var path = FindFile(language, @namespace);

        if (path == null)
            return null;

        string content;

        using (var reader = new StreamReader(path, Encoding))
            content = await reader.ReadToEndAsync().ConfigureAwait(false);

        var builder = _treeBuilderFactory.Create();
        builder.Namespace = @namespace;

        return YamlTranslationReader.Read(content, builder);
    }

    /// <summary>
    ///     Finds the file of a namespace, preferring <c>.yaml</c> over <c>.yml</c> and the language folder over the
    ///     folder of the language part.
    /// </summary>
    /// <param name="language">The requested language.</param>
    /// <param name="namespace">The requested namespace.</param>
    /// <returns>The path of the file or <c>null</c> if there is none.</returns>
    protected virtual string FindFile(string language, string @namespace)
    {
        return FindFileInDirectory(Path.Combine(BasePath, language), @namespace)
               ?? FindFileInDirectory(Path.Combine(BasePath, BackendUtilities.GetLanguagePart(language)), @namespace);
    }

    private static string FindFileInDirectory(string directory, string @namespace)
    {
        foreach (var extension in new[] { ".yaml", ".yml" })
        {
            var path = Path.Combine(directory, @namespace + extension);

            if (File.Exists(path))
                return path;
        }

        return null;
    }
}
