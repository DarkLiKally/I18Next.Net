using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

using I18Next.Net.Logging;

namespace I18Next.Net.Plugins;

/// <summary>
///     Sends missing keys to a server like the <c>saveMissing</c> option of the i18next-http-backend. The body is a JSON
///     object with the key and its default value.
/// </summary>
public class HttpMissingKeyHandler : IMissingKeyHandler
{
    /// <summary>
    ///     The default <see cref="AddPath" />, <c>locales/add/{{lng}}/{{ns}}</c>, like the i18next-http-backend.
    /// </summary>
    public const string DefaultAddPath = "locales/add/{{lng}}/{{ns}}";

    private static readonly Lazy<HttpClient> SharedHttpClient = new(() => new HttpClient());

    private readonly ConcurrentDictionary<(string Language, string Namespace, string Key), bool> _handledKeys = new();
    private readonly Func<HttpClient> _httpClientProvider;

    /// <summary>
    ///     Creates a handler posting the missing keys with a shared <see cref="HttpClient" />. The client has no base address,
    ///     so <paramref name="addPath" /> has to be an absolute url.
    /// </summary>
    /// <param name="addPath">The absolute url the missing keys are posted to, see <see cref="AddPath" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="addPath" /> is null.</exception>
    public HttpMissingKeyHandler(string addPath = DefaultAddPath)
        : this(() => SharedHttpClient.Value, addPath)
    {
    }

    /// <summary>
    ///     Creates a handler posting the missing keys with the given http client.
    /// </summary>
    /// <param name="httpClient">The http client used to post the missing keys.</param>
    /// <param name="addPath">The path or url the missing keys are posted to, see <see cref="AddPath" />.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="httpClient" /> or <paramref name="addPath" /> is null.
    /// </exception>
    public HttpMissingKeyHandler(HttpClient httpClient, string addPath = DefaultAddPath)
        : this(httpClient == null ? throw new ArgumentNullException(nameof(httpClient)) : () => httpClient, addPath)
    {
    }

    /// <summary>
    ///     Creates a handler posting the missing keys with the http client returned by the provider, e.g. a client of an
    ///     <c>IHttpClientFactory</c>.
    /// </summary>
    /// <param name="httpClientProvider">Provides the http client for each request.</param>
    /// <param name="addPath">The path or url the missing keys are posted to, see <see cref="AddPath" />.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="httpClientProvider" /> or <paramref name="addPath" /> is null.
    /// </exception>
    public HttpMissingKeyHandler(Func<HttpClient> httpClientProvider, string addPath = DefaultAddPath)
    {
        _httpClientProvider = httpClientProvider ?? throw new ArgumentNullException(nameof(httpClientProvider));
        AddPath = addPath ?? throw new ArgumentNullException(nameof(addPath));
    }

    /// <summary>
    ///     The path or url the missing keys are posted to. <c>{{lng}}</c> and <c>{{ns}}</c> are replaced with the language
    ///     and the namespace. Relative paths are resolved against the base address of the http client.
    /// </summary>
    public string AddPath { get; set; }

    /// <summary>
    ///     Headers added to each request, e.g. for authorization.
    /// </summary>
    public IDictionary<string, string> CustomHeaders { get; } = new Dictionary<string, string>();

    /// <summary>
    ///     The logger used to report failed requests. Defaults to a <see cref="TraceLogger" />.
    /// </summary>
    public ILogger Logger { get; set; } = new TraceLogger();

    /// <inheritdoc />
    /// <remarks>
    ///     Each key is posted once per handler instance with its default value or, if there is none, the key itself as value.
    ///     Failed requests and timeouts are logged as warnings instead of thrown, and the key is posted again when it is
    ///     missing the next time.
    /// </remarks>
    public async Task HandleMissingKeyAsync(object sender, MissingKeyEventArgs args)
    {
        var handledKey = (args.Language, args.Namespace, args.Key);

        if (!_handledKeys.TryAdd(handledKey, true))
            return;

        var url = AddPath.Replace("{{lng}}", Uri.EscapeDataString(args.Language)).Replace("{{ns}}", Uri.EscapeDataString(args.Namespace));

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = CreateContent(args.Key, args.DefaultValue ?? args.Key) };

        foreach (var header in CustomHeaders)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        try
        {
            using var response = await _httpClientProvider().SendAsync(request).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _handledKeys.TryRemove(handledKey, out _);

            if (Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning(ex, "Sending the missing key {namespace}:{key} in language {language} failed.", args.Namespace, args.Key, args.Language);
        }
    }

    private static ByteArrayContent CreateContent(string key, string value)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString(key, value);
            writer.WriteEndObject();
        }

        var content = new ByteArrayContent(stream.ToArray());
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        return content;
    }
}
