using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Loads translations from INI files, <c>{basePath}/{language}/{namespace}.ini</c>. Keys of a <c>[section]</c> are
///     prefixed with the section name, e.g. <c>menu.open</c> for the key <c>open</c> in the section <c>menu</c>; keys before
///     the first section have no prefix. See <see cref="SimpleIniParser" /> for the supported syntax.
///     When the file of a regional language like <c>de-CH</c> does not exist, the file of its language part
///     (<c>de</c>) is loaded.
/// </summary>
/// <param name="basePath">The directory containing a subdirectory per language.</param>
/// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
public class IniFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory) : ITranslationBackend
{
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory = treeBuilderFactory;

    /// <summary>
    ///     Creates a backend loading INI files from the given directory into hierarchical translation trees.
    /// </summary>
    /// <param name="basePath">The directory containing a subdirectory per language.</param>
    public IniFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    /// <summary>
    ///     Creates a backend loading INI files from the <c>locales</c> directory with the given tree builders.
    /// </summary>
    /// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
    public IniFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("locales", treeBuilderFactory)
    {
    }

    /// <summary>
    ///     Creates a backend loading INI files from the <c>locales</c> directory into hierarchical translation trees.
    /// </summary>
    public IniFileBackend()
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

        string iniContent;

        using (var reader = new StreamReader(path, Encoding))
        {
            iniContent = await reader.ReadToEndAsync().ConfigureAwait(false);
        }

        var iniReader = new SimpleIniParser(iniContent);

        var builder = _treeBuilderFactory.Create();

        PopulateTreeBuilder(iniReader, builder);

        return builder.Build();
    }

    /// <summary>
    ///     Finds the file of a namespace. Override it to use another file layout.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>
    ///     <c>{basePath}/{language}/{namespace}.ini</c>, else the file of the language part, or null if neither exists.
    /// </returns>
    protected virtual string FindFile(string language, string @namespace)
    {
        var path = Path.Combine(BasePath, language, @namespace + ".ini");

        if (File.Exists(path))
            return path;

        path = Path.Combine(BasePath, BackendUtilities.GetLanguagePart(language), @namespace + ".ini");

        return !File.Exists(path) ? null : path;
    }

    private static void PopulateTreeBuilder(SimpleIniParser iniReader, ITranslationTreeBuilder builder)
    {
        foreach (var iniSection in iniReader.GetSections().Concat(new[] { "" }))
        {
            var section = iniSection;

            if (section != string.Empty)
                section += ".";

            foreach (var iniKey in iniReader.GetKeys(iniSection))
            {
                var key = section + iniKey;
                var value = iniReader.GetValue(iniSection, iniKey, string.Empty);

                builder.AddTranslation(key, value);
            }
        }
    }
}
