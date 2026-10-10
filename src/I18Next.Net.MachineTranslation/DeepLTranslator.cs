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
///     Translates texts with the DeepL API v2. Keys ending with <c>:fx</c> use the DeepL API Free endpoint.
/// </summary>
public class DeepLTranslator : IMachineTranslator
{
    public const string FreeEndpoint = "https://api-free.deepl.com/";

    public const string ProEndpoint = "https://api.deepl.com/";

    private const string ServiceName = "DeepL";
    private const int MaxTextsPerRequest = 50;
    private const int MaxCharactersPerRequest = 30000;

    private static readonly HashSet<string> TargetVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        "en-GB", "en-US", "es-419", "pt-BR", "pt-PT", "zh-Hans", "zh-Hant"
    };

    private static readonly Lazy<HttpClient> SharedHttpClient = new(() => new HttpClient());

    private readonly string _authKey;
    private readonly HttpClient _httpClient;

    public DeepLTranslator(string authKey)
        : this(SharedHttpClient.Value, authKey)
    {
    }

    public DeepLTranslator(HttpClient httpClient, string authKey)
    {
        if (string.IsNullOrWhiteSpace(authKey))
            throw new ArgumentException("The DeepL auth key cannot be null, empty or whitespace string.", nameof(authKey));

        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authKey = authKey;
        Endpoint = new Uri(authKey.EndsWith(":fx", StringComparison.Ordinal) ? FreeEndpoint : ProEndpoint);
    }

    /// <summary>
    ///     The base address of the API, detected from the auth key by default.
    /// </summary>
    public Uri Endpoint { get; set; }

    /// <summary>
    ///     The formality of the translations like <c>more</c>, <c>less</c>, <c>prefer_more</c> or <c>prefer_less</c>.
    /// </summary>
    public string Formality { get; set; }

    /// <summary>
    ///     Maps i18next language codes to DeepL language codes, e.g. <c>no</c> to <c>NB</c>. Unmapped regional languages are
    ///     sent without the region unless DeepL supports the variant as a target language.
    /// </summary>
    public IDictionary<string, string> LanguageMappings { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<string>> TranslateAsync(IReadOnlyList<string> texts, string sourceLanguage, string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        if (texts == null)
            throw new ArgumentNullException(nameof(texts));
        if (string.IsNullOrWhiteSpace(targetLanguage))
            throw new ArgumentException("Target language cannot be null, empty or whitespace string.", nameof(targetLanguage));

        var protectedTexts = texts.Select(t => PlaceholderProtector.Xml.Protect(t ?? string.Empty)).ToList();
        var requestTexts = protectedTexts.Select(t => t.Text).ToList();
        var results = new string[texts.Count];

        foreach (var (start, count) in TranslationRequests.Split(requestTexts, MaxTextsPerRequest, MaxCharactersPerRequest))
        {
            var translations = await SendAsync(requestTexts.GetRange(start, count), sourceLanguage, targetLanguage, cancellationToken).ConfigureAwait(false);

            for (var i = 0; i < count; i++)
                results[start + i] = PlaceholderProtector.Xml.Restore(translations[i], protectedTexts[start + i]);
        }

        return results;
    }

    protected virtual string GetSourceLanguageCode(string language)
    {
        return LanguageMappings.TryGetValue(language, out var code) ? code : BackendUtilities.GetLanguagePart(language).ToUpperInvariant();
    }

    protected virtual string GetTargetLanguageCode(string language)
    {
        if (LanguageMappings.TryGetValue(language, out var code))
            return code;

        return (TargetVariants.Contains(language) ? language : BackendUtilities.GetLanguagePart(language)).ToUpperInvariant();
    }

    private async Task<List<string>> SendAsync(List<string> texts, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Endpoint, "v2/translate"));
        request.Headers.TryAddWithoutValidation("Authorization", "DeepL-Auth-Key " + _authKey);
        request.Content = TranslationRequests.CreateJsonContent(writer =>
        {
            writer.WriteStartObject();
            writer.WriteStartArray("text");

            foreach (var text in texts)
                writer.WriteStringValue(text);

            writer.WriteEndArray();

            if (!string.IsNullOrEmpty(sourceLanguage))
                writer.WriteString("source_lang", GetSourceLanguageCode(sourceLanguage));

            writer.WriteString("target_lang", GetTargetLanguageCode(targetLanguage));
            writer.WriteString("tag_handling", "xml");
            writer.WriteStartArray("ignore_tags");
            writer.WriteStringValue(PlaceholderProtector.Xml.Element);
            writer.WriteEndArray();

            if (!string.IsNullOrEmpty(Formality))
                writer.WriteString("formality", Formality);

            writer.WriteEndObject();
        });

        using var document = await TranslationRequests.SendAsync(_httpClient, request, ServiceName, cancellationToken).ConfigureAwait(false);

        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("translations", out var translations)
            || translations.ValueKind != JsonValueKind.Array
            || translations.GetArrayLength() != texts.Count)
        {
            throw TranslationRequests.UnexpectedResponse(ServiceName);
        }

        var result = new List<string>(texts.Count);

        foreach (var translation in translations.EnumerateArray())
        {
            if (translation.ValueKind != JsonValueKind.Object || !translation.TryGetProperty("text", out var text) || text.ValueKind != JsonValueKind.String)
                throw TranslationRequests.UnexpectedResponse(ServiceName);

            result.Add(text.GetString());
        }

        return result;
    }
}
