using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.MachineTranslation.Internal;

namespace I18Next.Net.MachineTranslation;

/// <summary>
///     Translates texts with the Azure AI Translator REST API v3.
/// </summary>
public class AzureTranslator : IMachineTranslator
{
    public const string DefaultEndpoint = "https://api.cognitive.microsofttranslator.com/";

    private const string ServiceName = "Azure AI Translator";
    private const int MaxTextsPerRequest = 100;
    private const int MaxCharactersPerRequest = 40000;

    private static readonly HashSet<string> LanguageVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        "fr-CA", "iu-Latn", "mn-Cyrl", "mn-Mong", "pt-PT", "sr-Cyrl", "sr-Latn", "tlh-Latn", "tlh-Piqd", "zh-Hans", "zh-Hant"
    };

    private static readonly Lazy<HttpClient> SharedHttpClient = new(() => new HttpClient());

    private readonly HttpClient _httpClient;
    private readonly string _key;
    private readonly string _region;

    public AzureTranslator(string key, string region = null)
        : this(SharedHttpClient.Value, key, region)
    {
    }

    public AzureTranslator(HttpClient httpClient, string key, string region = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("The Azure AI Translator key cannot be null, empty or whitespace string.", nameof(key));

        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _key = key;
        _region = region;
    }

    /// <summary>
    ///     The base address of the API. <c>translate</c> is appended, so custom endpoints need the full path, e.g.
    ///     <c>https://{resource}.cognitiveservices.azure.com/translator/text/v3.0/</c>.
    /// </summary>
    public Uri Endpoint { get; set; } = new(DefaultEndpoint);

    /// <summary>
    ///     Maps i18next language codes to Azure language codes, e.g. <c>zh-CN</c> to <c>zh-Hans</c>. Unmapped regional
    ///     languages are sent without the region unless Azure supports the variant.
    /// </summary>
    public IDictionary<string, string> LanguageMappings { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<string>> TranslateAsync(IReadOnlyList<string> texts, string sourceLanguage, string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        if (texts == null)
            throw new ArgumentNullException(nameof(texts));
        if (string.IsNullOrWhiteSpace(targetLanguage))
            throw new ArgumentException("Target language cannot be null, empty or whitespace string.", nameof(targetLanguage));

        var protectedTexts = texts.Select(t => PlaceholderProtector.Html.Protect(t ?? string.Empty)).ToList();
        var requestTexts = protectedTexts.Select(t => t.Text).ToList();
        var results = new string[texts.Count];

        foreach (var (start, count) in TranslationRequests.Split(requestTexts, MaxTextsPerRequest, MaxCharactersPerRequest))
        {
            var translations = await SendAsync(requestTexts.GetRange(start, count), sourceLanguage, targetLanguage, cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < count; i++)
                results[start + i] = PlaceholderProtector.Html.Restore(translations[i], protectedTexts[start + i]);
        }

        return results;
    }

    protected virtual string GetLanguageCode(string language)
    {
        if (LanguageMappings.TryGetValue(language, out var code))
            return code;

        return LanguageVariants.Contains(language) ? language : BackendUtilities.GetLanguagePart(language);
    }

    private async Task<List<string>> SendAsync(List<string> texts, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        var query = "translate?api-version=3.0&textType=html&to=" + Uri.EscapeDataString(GetLanguageCode(targetLanguage));

        if (!string.IsNullOrEmpty(sourceLanguage))
            query += "&from=" + Uri.EscapeDataString(GetLanguageCode(sourceLanguage));

        var endpoint = Endpoint.AbsoluteUri.EndsWith("/", StringComparison.Ordinal) ? Endpoint : new Uri(Endpoint.AbsoluteUri + "/");

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, query));
        request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", _key);

        if (!string.IsNullOrEmpty(_region))
            request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Region", _region);

        request.Content = TranslationRequests.CreateJsonContent(writer =>
        {
            writer.WriteStartArray();

            foreach (var text in texts)
            {
                writer.WriteStartObject();
                writer.WriteString("Text", text);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        });

        using var document = await TranslationRequests.SendAsync(_httpClient, request, ServiceName, cancellationToken).ConfigureAwait(false);

        if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() != texts.Count)
            throw TranslationRequests.UnexpectedResponse(ServiceName);

        var result = new List<string>(texts.Count);

        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object
                || !item.TryGetProperty("translations", out var translations)
                || translations.ValueKind != JsonValueKind.Array
                || translations.GetArrayLength() == 0
                || translations[0].ValueKind != JsonValueKind.Object
                || !translations[0].TryGetProperty("text", out var text)
                || text.ValueKind != JsonValueKind.String)
            {
                throw TranslationRequests.UnexpectedResponse(ServiceName);
            }

            result.Add(text.GetString());
        }

        return result;
    }
}
