using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

internal static class JsonTranslationReader
{
    public static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static ITranslationTree Read(string json, ITranslationTreeBuilder builder)
    {
        using var document = JsonDocument.Parse(json, DocumentOptions);

        return Build(document, builder);
    }

    public static async Task<ITranslationTree> ReadAsync(Stream stream, ITranslationTreeBuilder builder)
    {
        using var document = await JsonDocument.ParseAsync(stream, DocumentOptions).ConfigureAwait(false);

        return Build(document, builder);
    }

    private static ITranslationTree Build(JsonDocument document, ITranslationTreeBuilder builder)
    {
        if (document.RootElement.ValueKind == JsonValueKind.Object)
            PopulateTreeBuilder("", document.RootElement, builder);

        return builder.Build();
    }

    private static void PopulateTreeBuilder(string path, JsonElement node, ITranslationTreeBuilder builder)
    {
        if (path != string.Empty)
            path += ".";

        foreach (var childNode in node.EnumerateObject())
            AddValue(path + childNode.Name, childNode.Value, builder);
    }

    private static void AddValue(string key, JsonElement value, ITranslationTreeBuilder builder)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                PopulateTreeBuilder(key, value, builder);
                break;
            case JsonValueKind.Array:
                var index = 0;

                foreach (var item in value.EnumerateArray())
                    AddValue($"{key}.{index++}", item, builder);

                break;
            case JsonValueKind.String:
                builder.AddTranslation(key, value.GetString());
                break;
            case JsonValueKind.Number:
                builder.AddTranslation(key, value.GetRawText());
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
