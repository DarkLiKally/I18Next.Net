using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

public class JsonFileBackend : ITranslationBackend
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory;

    public JsonFileBackend(string basePath)
        : this(basePath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    public JsonFileBackend(string basePath, ITranslationTreeBuilderFactory treeBuilderFactory)
    {
        BasePath = basePath;
        _treeBuilderFactory = treeBuilderFactory;
    }

    public JsonFileBackend(ITranslationTreeBuilderFactory treeBuilderFactory)
        : this("locales", treeBuilderFactory)
    {
    }

    public JsonFileBackend()
        : this("locales")
    {
    }

    protected string BasePath { get; }

    public Encoding Encoding { get; set; } = Encoding.UTF8;

    public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        var path = FindFile(language, @namespace);

        if (path == null)
            return null;

        var builder = _treeBuilderFactory.Create();

        using (var document = await ParseDocumentAsync(path).ConfigureAwait(false))
        {
            if (document.RootElement.ValueKind == JsonValueKind.Object)
                PopulateTreeBuilder("", document.RootElement, builder);
        }

        return builder.Build();
    }

    protected virtual string FindFile(string language, string @namespace)
    {
        var path = Path.Combine(BasePath, language, @namespace + ".json");

        if (File.Exists(path))
            return path;

        path = Path.Combine(BasePath, BackendUtilities.GetLanguagePart(language), @namespace + ".json");

        return !File.Exists(path) ? null : path;
    }

    private async Task<JsonDocument> ParseDocumentAsync(string path)
    {
        if (Encoding.CodePage == Encoding.UTF8.CodePage)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                return await JsonDocument.ParseAsync(stream, DocumentOptions).ConfigureAwait(false);
        }

        string content;

        using (var streamReader = new StreamReader(path, Encoding))
            content = await streamReader.ReadToEndAsync().ConfigureAwait(false);

        return JsonDocument.Parse(content, DocumentOptions);
    }

    private static void PopulateTreeBuilder(string path, JsonElement node, ITranslationTreeBuilder builder)
    {
        if (path != string.Empty)
            path = path + ".";

        foreach (var childNode in node.EnumerateObject())
        {
            var key = path + childNode.Name;

            switch (childNode.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    PopulateTreeBuilder(key, childNode.Value, builder);
                    break;
                case JsonValueKind.String:
                    builder.AddTranslation(key, childNode.Value.GetString());
                    break;
                case JsonValueKind.Number:
                    builder.AddTranslation(key, childNode.Value.GetRawText());
                    break;
                case JsonValueKind.True:
                    builder.AddTranslation(key, bool.TrueString);
                    break;
                case JsonValueKind.False:
                    builder.AddTranslation(key, bool.FalseString);
                    break;
            }
        }
    }
}