using System.IO;
using System.Text;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Yaml;

/// <summary>
///     Loads translations from <c>{basePath}/{language}/{namespace}.yaml</c> or <c>.yml</c> files.
/// </summary>
public class YamlFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory) : ITranslationBackend
{
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory = treeBuilderFactory;

    public YamlFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    public YamlFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("locales", treeBuilderFactory)
    {
    }

    public YamlFileBackend()
        : this("locales")
    {
    }

    protected string BasePath { get; } = basePath;

    public Encoding Encoding { get; set; } = Encoding.UTF8;

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
