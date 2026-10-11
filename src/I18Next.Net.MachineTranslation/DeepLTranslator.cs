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
    /// <summary>
    ///     The endpoint of the DeepL API Free, used for auth keys ending with <c>:fx</c>.
    /// </summary>
    public const string FreeEndpoint = "https://api-free.deepl.com/";

    /// <summary>
    ///     The endpoint of the DeepL API Pro, used for all other auth keys.
    /// </summary>
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

    /// <summary>
    ///     Creates a translator which sends its requests with an <see cref="HttpClient" /> shared by all translators
    ///     created this way.
    /// </summary>
    /// <param name="authKey">The DeepL authentication key.</param>
    /// <exception cref="ArgumentException">If <paramref name="authKey" /> is <c>null</c>, empty or whitespace.</exception>
    public DeepLTranslator(string authKey)
        : this(SharedHttpClient.Value, authKey)
    {
    }

    /// <summary>
    ///     Creates a translator which sends its requests with the given <see cref="HttpClient" />, e.g. one created by an
    ///     <c>IHttpClientFactory</c>.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to send the requests.</param>
    /// <param name="authKey">The DeepL authentication key.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="httpClient" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">If <paramref name="authKey" /> is <c>null</c>, empty or whitespace.</exception>
    public DeepLTranslator(HttpClient httpClient, string authKey)
    {
        if (string.IsNullOrWhiteSpace(authKey))
            throw new ArgumentException("The DeepL auth key cannot be null, empty or whitespace string.", nameof(authKey));

        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authKey = authKey;
        Endpoint = new Uri(authKey.EndsWith(":fx", StringComparison.Ordinal) ? FreeEndpoint : ProEndpoint);
    }

    /// <summary>
    ///     The base address of the API. Defaults to <see cref="FreeEndpoint" /> for auth keys ending with <c>:fx</c> and to
    ///     <see cref="ProEndpoint" /> otherwise.
    /// </summary>
    public Uri Endpoint { get; set; }

    /// <summary>
    ///     The formality of the translations like <c>more</c>, <c>less</c>, <c>prefer_more</c> or <c>prefer_less</c>, or
    ///     <c>null</c> to use the default formality of DeepL.
    /// </summary>
    public string Formality { get; set; }

    /// <summary>
    ///     Maps i18next language codes to DeepL language codes, e.g. <c>no</c> to <c>NB</c>. Unmapped regional languages are
    ///     sent without the region unless DeepL supports the variant as a target language.
    /// </summary>
    public IDictionary<string, string> LanguageMappings { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    /// <remarks>
    ///     i18next interpolations like <c>{{name}}</c>, nestings like <c>$t(key)</c> and HTML tags are wrapped in ignored XML
    ///     tags, so DeepL keeps them unchanged. The texts are sent in batches of at most 50 texts and 30,000 characters,
    ///     <c>null</c> texts are translated as empty strings.
    /// </remarks>
    /// <exception cref="ArgumentNullException">If <paramref name="texts" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    ///     If <paramref name="targetLanguage" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    /// <exception cref="MachineTranslationException">
    ///     If the service rejects a request or returns an unexpected response.
    /// </exception>
    /// <exception cref="HttpRequestException">If the service cannot be reached.</exception>
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

    /// <summary>
    ///     Returns the DeepL code of a source language: the code of the <see cref="LanguageMappings" /> or else the upper
    ///     case language part, as DeepL does not accept regional source languages.
    /// </summary>
    /// <param name="language">The i18next language code, e.g. <c>en-US</c>.</param>
    /// <returns>The DeepL language code, e.g. <c>EN</c>.</returns>
    protected virtual string GetSourceLanguageCode(string language)
    {
        return LanguageMappings.TryGetValue(language, out var code) ? code : BackendUtilities.GetLanguagePart(language).ToUpperInvariant();
    }

    /// <summary>
    ///     Returns the DeepL code of a target language: the code of the <see cref="LanguageMappings" />, the upper case
    ///     variant if DeepL supports it as a target language (e.g. <c>EN-GB</c>, <c>PT-BR</c> or <c>ZH-HANS</c>) or else the
    ///     upper case language part.
    /// </summary>
    /// <param name="language">The i18next language code, e.g. <c>de-AT</c>.</param>
    /// <returns>The DeepL language code, e.g. <c>DE</c>.</returns>
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
