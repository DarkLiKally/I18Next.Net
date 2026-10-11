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
    /// <summary>
    ///     The global endpoint of the Azure AI Translator.
    /// </summary>
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

    /// <summary>
    ///     Creates a translator which sends its requests with an <see cref="HttpClient" /> shared by all translators
    ///     created this way.
    /// </summary>
    /// <param name="key">The key of the Translator resource.</param>
    /// <param name="region">
    ///     The region of the Translator resource, required for regional and multi-service resources, or <c>null</c> for a
    ///     global resource.
    /// </param>
    /// <exception cref="ArgumentException">If <paramref name="key" /> is <c>null</c>, empty or whitespace.</exception>
    public AzureTranslator(string key, string region = null)
        : this(SharedHttpClient.Value, key, region)
    {
    }

    /// <summary>
    ///     Creates a translator which sends its requests with the given <see cref="HttpClient" />, e.g. one created by an
    ///     <c>IHttpClientFactory</c>.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to send the requests.</param>
    /// <param name="key">The key of the Translator resource.</param>
    /// <param name="region">
    ///     The region of the Translator resource, required for regional and multi-service resources, or <c>null</c> for a
    ///     global resource.
    /// </param>
    /// <exception cref="ArgumentNullException">If <paramref name="httpClient" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">If <paramref name="key" /> is <c>null</c>, empty or whitespace.</exception>
    public AzureTranslator(HttpClient httpClient, string key, string region = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("The Azure AI Translator key cannot be null, empty or whitespace string.", nameof(key));

        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _key = key;
        _region = region;
    }

    /// <summary>
    ///     The base address of the API. Defaults to <see cref="DefaultEndpoint" />. <c>translate</c> is appended, so custom
    ///     endpoints need the full path, e.g. <c>https://{resource}.cognitiveservices.azure.com/translator/text/v3.0/</c>.
    /// </summary>
    public Uri Endpoint { get; set; } = new(DefaultEndpoint);

    /// <summary>
    ///     Maps i18next language codes to Azure language codes, e.g. <c>zh-CN</c> to <c>zh-Hans</c>. Unmapped regional
    ///     languages are sent without the region unless Azure supports the variant.
    /// </summary>
    public IDictionary<string, string> LanguageMappings { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    /// <remarks>
    ///     The texts are translated as HTML. i18next interpolations like <c>{{name}}</c>, nestings like <c>$t(key)</c> and
    ///     HTML tags are wrapped in <c>notranslate</c> elements, so Azure keeps them unchanged. The texts are sent in batches
    ///     of at most 100 texts and 40,000 characters, <c>null</c> texts are translated as empty strings.
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

    /// <summary>
    ///     Returns the Azure code of a language: the code of the <see cref="LanguageMappings" />, the variant if Azure
    ///     supports it (e.g. <c>fr-CA</c>, <c>pt-PT</c> or <c>zh-Hans</c>) or else the language part.
    /// </summary>
    /// <param name="language">The i18next language code, e.g. <c>de-AT</c>.</param>
    /// <returns>The Azure language code, e.g. <c>de</c>.</returns>
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
