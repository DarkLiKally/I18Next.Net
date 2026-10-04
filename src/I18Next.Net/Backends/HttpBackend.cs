using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Loads translations over HTTP, similar to the i18next-http-backend.
/// </summary>
public class HttpBackend : ITranslationBackend
{
    public const string DefaultLoadPath = "locales/{{lng}}/{{ns}}.json";

    public const string HttpClientName = "I18Next.Net.HttpBackend";

    private static readonly Lazy<HttpClient> SharedHttpClient = new(() => new HttpClient());

    private readonly Func<HttpClient> _httpClientProvider;
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory;

    public HttpBackend(string loadPath = DefaultLoadPath)
        : this(() => SharedHttpClient.Value, loadPath)
    {
    }

    public HttpBackend(HttpClient httpClient, string loadPath = DefaultLoadPath)
        : this(httpClient == null ? throw new ArgumentNullException(nameof(httpClient)) : () => httpClient, loadPath)
    {
    }

    public HttpBackend(Func<HttpClient> httpClientProvider, string loadPath = DefaultLoadPath)
        : this(httpClientProvider, loadPath, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    public HttpBackend(Func<HttpClient> httpClientProvider, string loadPath, ITranslationTreeBuilderFactory treeBuilderFactory)
    {
        _httpClientProvider = httpClientProvider ?? throw new ArgumentNullException(nameof(httpClientProvider));
        _treeBuilderFactory = treeBuilderFactory ?? throw new ArgumentNullException(nameof(treeBuilderFactory));
        LoadPath = loadPath ?? throw new ArgumentNullException(nameof(loadPath));
    }

    /// <summary>
    ///     The path or url the namespaces are loaded from. <c>{{lng}}</c> and <c>{{ns}}</c> are replaced with the language and
    ///     the namespace. Relative paths are resolved against the base address of the http client.
    /// </summary>
    public string LoadPath { get; set; }

    /// <summary>
    ///     Resolves the load path for a language and namespace instead of <see cref="LoadPath" />.
    /// </summary>
    public Func<string, string, string> LoadPathResolver { get; set; }

    public IDictionary<string, string> QueryStringParams { get; } = new Dictionary<string, string>();

    public IDictionary<string, string> CustomHeaders { get; } = new Dictionary<string, string>();

    /// <summary>
    ///     Loads the language part of a regional language like "de" for "de-DE" when the regional namespace does not exist.
    /// </summary>
    public bool FallbackToLanguagePart { get; set; } = true;

    /// <summary>
    ///     Parses the response content into the provided tree builder. The content is parsed as JSON by default.
    /// </summary>
    public Func<string, ITranslationTreeBuilder, ITranslationTree> Parse { get; set; }

    public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        var tree = await LoadAsync(language, @namespace).ConfigureAwait(false);

        if (tree != null || !FallbackToLanguagePart)
            return tree;

        var languagePart = BackendUtilities.GetLanguagePart(language);

        return languagePart == language ? null : await LoadAsync(languagePart, @namespace).ConfigureAwait(false);
    }

    protected virtual string GetUrl(string language, string @namespace)
    {
        var path = LoadPathResolver?.Invoke(language, @namespace) ?? LoadPath;
        var url = path.Replace("{{lng}}", Uri.EscapeDataString(language)).Replace("{{ns}}", Uri.EscapeDataString(@namespace));

        if (QueryStringParams.Count == 0)
            return url;

        var query = string.Join("&", QueryStringParams.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value ?? string.Empty)}"));

        return url + (url.IndexOf('?') < 0 ? "?" : "&") + query;
    }

    private async Task<ITranslationTree> LoadAsync(string language, string @namespace)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, GetUrl(language, @namespace));

        foreach (var header in CustomHeaders)
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        using var response = await _httpClientProvider().SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var builder = _treeBuilderFactory.Create();
        builder.Namespace = @namespace;

        if (Parse != null)
            return Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false), builder);

        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        return await JsonTranslationReader.ReadAsync(stream, builder).ConfigureAwait(false);
    }
}
