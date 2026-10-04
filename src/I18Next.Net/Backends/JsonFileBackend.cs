using System.IO;
using System.Text;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

public class JsonFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory) : ITranslationBackend
{
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory = treeBuilderFactory;

    public JsonFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    public JsonFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("locales", treeBuilderFactory)
    {
    }

    public JsonFileBackend()
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

    protected virtual string FindFile(string language, string @namespace)
    {
        var path = Path.Combine(BasePath, language, @namespace + ".json");

        if (File.Exists(path))
            return path;

        path = Path.Combine(BasePath, BackendUtilities.GetLanguagePart(language), @namespace + ".json");

        return !File.Exists(path) ? null : path;
    }
}
