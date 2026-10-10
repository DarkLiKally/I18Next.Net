using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using I18Next.Net.Backends;

using Microsoft.AspNetCore.Http;

namespace I18Next.Net.AspNetCore;

/// <summary>
///     Translates paths between the canonical routes of the application and localized paths with a language prefix, e.g.
///     <c>/products/42</c> and <c>/de/produkte/42</c>. The translated path segments are loaded from a namespace of the
///     translation backend and cached until the backend reports changed translations.
/// </summary>
public class I18NextRouteLocalizer
{
    private static readonly char[] InvalidSegmentCharacters = ['/', '\\', '?', '#'];
    private static readonly char[] PathSuffixStart = ['?', '#'];

    private readonly ITranslationBackend _backend;
    private readonly Dictionary<string, string> _languages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<string, Task<RouteMap>> _loadMap;
    private readonly ConcurrentDictionary<string, Task<RouteMap>> _maps = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(string Language, PathString Prefix)> _prefixes = [];

    public I18NextRouteLocalizer(ITranslationBackend backend, I18NextLocalizedRoutesOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (options.Languages == null || options.Languages.Count == 0)
            throw new ArgumentException("Please supply at least one language.", nameof(options));
        if (string.IsNullOrEmpty(options.Namespace))
            throw new ArgumentException("Namespace cannot be null or empty.", nameof(options));

        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _loadMap = LoadMapAsync;

        var languages = new List<string>();

        foreach (var language in options.Languages)
        {
            if (!IsValidLanguage(language))
                throw new ArgumentException($"The language `{language}` is not a valid language.", nameof(options));

            if (_languages.ContainsKey(language))
                continue;

            _languages.Add(language, language);
            _prefixes.Add((language, new PathString("/" + language)));
            languages.Add(language);
        }

        Languages = languages;
        Namespace = options.Namespace;
        DefaultLanguage = options.DefaultLanguage == null
            ? languages[0]
            : MatchLanguage(options.DefaultLanguage) ?? throw new ArgumentException("The default language must be one of the languages.", nameof(options));

        if (backend is INotifyingTranslationBackend notifyingBackend)
            notifyingBackend.TranslationsChanged += OnTranslationsChanged;
    }

    /// <summary>
    ///     The languages used as path prefix.
    /// </summary>
    public IReadOnlyList<string> Languages { get; }

    /// <summary>
    ///     The language used when no other language matches.
    /// </summary>
    public string DefaultLanguage { get; }

    /// <summary>
    ///     The namespace containing the translated path segments.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    ///     Finds the configured language for a language or culture name. Regional languages match the configured language
    ///     without region and the other way around, e.g. <c>de-AT</c> matches <c>de</c>.
    /// </summary>
    /// <param name="language">The language or culture name.</param>
    /// <returns>The configured language or <c>null</c> if no configured language matches.</returns>
    public string MatchLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
            return null;

        if (_languages.TryGetValue(language, out var configuredLanguage))
            return configuredLanguage;

        var languagePart = BackendUtilities.GetLanguagePart(language);

        if (_languages.TryGetValue(languagePart, out configuredLanguage))
            return configuredLanguage;

        foreach (var candidate in Languages)
        {
            if (string.Equals(BackendUtilities.GetLanguagePart(candidate), languagePart, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        return null;
    }

    /// <summary>
    ///     Translates the segments of a canonical path and adds the language prefix, e.g. <c>/products/42?page=2</c>
    ///     becomes <c>/de/produkte/42?page=2</c>. Segments without translation, the query string and the fragment are kept.
    /// </summary>
    /// <param name="path">The canonical, URL encoded path starting with a slash.</param>
    /// <param name="language">The language of the localized path.</param>
    /// <returns>The URL encoded localized path.</returns>
    public string LocalizePath(string path, string language)
    {
        return LocalizePathAsync(path, language).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <inheritdoc cref="LocalizePath" />
    public async Task<string> LocalizePathAsync(string path, string language)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (path.Length > 0 && path[0] != '/')
            throw new ArgumentException("The path must start with a slash.", nameof(path));

        var matchedLanguage = MatchLanguage(language) ?? throw new ArgumentException($"The language `{language}` is not configured.", nameof(language));
        var suffixIndex = path.IndexOfAny(PathSuffixStart);
        var pathPart = suffixIndex < 0 ? path : path.Substring(0, suffixIndex);
        var map = await GetMapAsync(matchedLanguage).ConfigureAwait(false);
        var result = new StringBuilder(path.Length + matchedLanguage.Length + 1).Append('/').Append(matchedLanguage);

        if (pathPart.Length > 1)
        {
            var segments = pathPart.Split('/');

            for (var i = 1; i < segments.Length; i++)
                result.Append('/').Append(LocalizeSegment(map, segments[i]));
        }

        if (suffixIndex >= 0)
            result.Append(path, suffixIndex, path.Length - suffixIndex);

        return result.ToString();
    }

    internal bool TryGetLanguagePrefix(PathString path, out string language, out PathString matched, out PathString remaining)
    {
        foreach (var prefix in _prefixes)
        {
            if (path.StartsWithSegments(prefix.Prefix, StringComparison.OrdinalIgnoreCase, out matched, out remaining))
            {
                language = prefix.Language;

                return true;
            }
        }

        language = null;
        matched = default;
        remaining = default;

        return false;
    }

    internal async Task<PathString> GetCanonicalPathAsync(PathString path, string language)
    {
        if (!path.HasValue || path.Value.Length < 2)
            return path;

        var map = await GetMapAsync(language).ConfigureAwait(false);

        if (map.Canonical.Count == 0)
            return path;

        var segments = path.Value.Split('/');
        var changed = false;

        for (var i = 1; i < segments.Length; i++)
        {
            if (segments[i].Length > 0 && map.Canonical.TryGetValue(segments[i], out var canonicalSegment))
            {
                segments[i] = canonicalSegment;
                changed = true;
            }
        }

        return changed ? new PathString(string.Join("/", segments)) : path;
    }

    internal async Task LoadAsync()
    {
        foreach (var language in Languages)
            await GetMapAsync(language).ConfigureAwait(false);
    }

    private async Task<RouteMap> GetMapAsync(string language)
    {
        var task = _maps.GetOrAdd(language, _loadMap);

        try
        {
            return await task.ConfigureAwait(false);
        }
        catch
        {
            ((ICollection<KeyValuePair<string, Task<RouteMap>>>)_maps).Remove(new KeyValuePair<string, Task<RouteMap>>(language, task));

            throw;
        }
    }

    private async Task<RouteMap> LoadMapAsync(string language)
    {
        var map = new RouteMap();
        var tree = await _backend.LoadNamespaceAsync(language, Namespace).ConfigureAwait(false);

        if (tree == null)
            return map;

        foreach (var entry in tree.GetAllValues())
        {
            if (!IsValidSegment(entry.Key) || !IsValidSegment(entry.Value))
                continue;

            if (!map.Localized.ContainsKey(entry.Key))
                map.Localized.Add(entry.Key, entry.Value);

            if (!map.Canonical.ContainsKey(entry.Value))
                map.Canonical.Add(entry.Value, entry.Key);
        }

        return map;
    }

    private void OnTranslationsChanged(object sender, TranslationsChangedEventArgs e)
    {
        foreach (var language in _maps.Keys)
        {
            if (e.Affects(language, Namespace))
                _maps.TryRemove(language, out _);
        }
    }

    private static string LocalizeSegment(RouteMap map, string segment)
    {
        if (segment.Length == 0 || !map.Localized.TryGetValue(Uri.UnescapeDataString(segment), out var localizedSegment))
            return segment;

        return new PathString("/" + localizedSegment).ToUriComponent().Substring(1);
    }

    private static bool IsValidLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
            return false;

        foreach (var c in language)
        {
            if (c is not (>= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_'))
                return false;
        }

        return true;
    }

    private static bool IsValidSegment(string segment)
    {
        return !string.IsNullOrWhiteSpace(segment) && segment != "." && segment != ".." && segment.IndexOfAny(InvalidSegmentCharacters) < 0;
    }

    private sealed class RouteMap
    {
        public Dictionary<string, string> Canonical { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Localized { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
