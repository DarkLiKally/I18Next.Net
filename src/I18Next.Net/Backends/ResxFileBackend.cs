using System;
using System.IO;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Loads translations from .resx files, either <c>{basePath}/{language}/{namespace}.resx</c> or with the .NET naming
///     <c>{basePath}/{namespace}.{language}.resx</c>, where <c>{basePath}/{namespace}.resx</c> contains the
///     <see cref="NeutralLanguage" />. Only string resources are loaded.
/// </summary>
/// <param name="basePath">The directory containing the .resx files.</param>
/// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
public class ResxFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory) : ITranslationBackend
{
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory = treeBuilderFactory;

    /// <summary>
    ///     Creates a backend loading .resx files from the given directory into hierarchical translation trees.
    /// </summary>
    /// <param name="basePath">The directory containing the .resx files.</param>
    public ResxFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    /// <summary>
    ///     Creates a backend loading .resx files from the <c>locales</c> directory with the given tree builders.
    /// </summary>
    /// <param name="treeBuilderFactory">Creates the builders of the loaded translation trees.</param>
    public ResxFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("locales", treeBuilderFactory)
    {
    }

    /// <summary>
    ///     Creates a backend loading .resx files from the <c>locales</c> directory into hierarchical translation trees.
    /// </summary>
    public ResxFileBackend()
        : this("locales")
    {
    }

    /// <summary>
    ///     The directory containing the .resx files. Relative paths are resolved against the current directory.
    /// </summary>
    protected string BasePath { get; } = basePath;

    /// <summary>
    ///     The language of the files without language in their name, e.g. <c>Strings.resx</c>.
    /// </summary>
    public string NeutralLanguage { get; set; } = "en";

    /// <inheritdoc />
    /// <remarks>Returns null if no file of the namespace exists for the language.</remarks>
    public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        var path = FindFile(language, @namespace);

        if (path == null)
            return Task.FromResult<ITranslationTree>(null);

        var builder = _treeBuilderFactory.Create();
        builder.Namespace = @namespace;

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        return Task.FromResult(ResxTranslationReader.Read(stream, builder));
    }

    /// <summary>
    ///     Finds the file of a namespace. Override it to use another file layout.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>
    ///     The file of the language, else the file of its language part, else the neutral file if the language part is the
    ///     <see cref="NeutralLanguage" />, or null if none exists.
    /// </returns>
    protected virtual string FindFile(string language, string @namespace)
    {
        var languagePart = BackendUtilities.GetLanguagePart(language);

        return FindFileForLanguage(language, @namespace)
               ?? FindFileForLanguage(languagePart, @namespace)
               ?? (string.Equals(languagePart, NeutralLanguage, StringComparison.OrdinalIgnoreCase) ? FindExisting(Path.Combine(BasePath, @namespace + ".resx")) : null);
    }

    private string FindFileForLanguage(string language, string @namespace)
    {
        return FindExisting(Path.Combine(BasePath, language, @namespace + ".resx")) ?? FindExisting(Path.Combine(BasePath, $"{@namespace}.{language}.resx"));
    }

    private static string FindExisting(string path)
    {
        return File.Exists(path) ? path : null;
    }
}
