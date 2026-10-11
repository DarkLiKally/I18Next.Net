using System;
using System.IO;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Loads translations from .resx files, either <c>{basePath}/{language}/{namespace}.resx</c> or with the .NET naming
///     <c>{basePath}/{namespace}.{language}.resx</c>, where <c>{basePath}/{namespace}.resx</c> contains the
///     <see cref="NeutralLanguage" />.
/// </summary>
public class ResxFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory) : ITranslationBackend
{
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory = treeBuilderFactory;

    public ResxFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    public ResxFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("locales", treeBuilderFactory)
    {
    }

    public ResxFileBackend()
        : this("locales")
    {
    }

    protected string BasePath { get; } = basePath;

    /// <summary>
    ///     The language of the files without language in their name, e.g. <c>Strings.resx</c>.
    /// </summary>
    public string NeutralLanguage { get; set; } = "en";

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
