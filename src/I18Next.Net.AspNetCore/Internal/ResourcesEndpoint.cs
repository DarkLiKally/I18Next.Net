#if NET6_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.Backends;

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace I18Next.Net.AspNetCore.Internal;

internal sealed class ResourcesEndpoint
{
    private const int MaxCachedResources = 250;
    private const int MaxMultiLoadResources = 100;

    private static readonly char[] MultiLoadSeparators = ['+', ' '];

    private readonly ITranslationBackend _backend;
    private readonly ConcurrentDictionary<(string Language, string Namespace), CachedResource> _cache = new();
    private readonly string _cacheControl;
    private readonly ResourceNameFilter _filter;
    private readonly bool _multiLoad;
    private int _version;

    public ResourcesEndpoint(ITranslationBackend backend, I18NextResourcesOptions options, bool multiLoad)
    {
        _backend = backend;
        _cacheControl = options.CacheControl;
        _filter = new ResourceNameFilter(options);
        _multiLoad = multiLoad;

        if (backend is INotifyingTranslationBackend notifyingBackend)
            notifyingBackend.TranslationsChanged += OnTranslationsChanged;
    }

    public async Task HandleAsync(HttpContext context)
    {
        var (statusCode, content) = _multiLoad ? await GetMultiLoadContentAsync(context) : await GetContentAsync(context);

        if (content == null)
        {
            context.Response.StatusCode = statusCode;

            return;
        }

        var eTag = new EntityTagHeaderValue(content.ETag);
        var headers = context.Response.GetTypedHeaders();
        headers.ETag = eTag;

        if (_cacheControl != null)
            context.Response.Headers.CacheControl = _cacheControl;

        if (IsNotModified(context.Request, eTag))
        {
            context.Response.StatusCode = StatusCodes.Status304NotModified;

            return;
        }

        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength = content.Json.Length;

        if (!HttpMethods.IsHead(context.Request.Method))
            await context.Response.Body.WriteAsync(content.Json, context.RequestAborted);
    }

    private async Task<(int StatusCode, CachedResource Content)> GetContentAsync(HttpContext context)
    {
        var language = _filter.GetLanguage(GetValue(context, "lng"));
        var ns = _filter.GetNamespace(GetValue(context, "ns"));

        if (language == null || ns == null)
            return (StatusCodes.Status404NotFound, null);

        var resource = await GetResourceAsync(language, ns);

        return resource == null ? (StatusCodes.Status404NotFound, null) : (StatusCodes.Status200OK, resource);
    }

    private async Task<(int StatusCode, CachedResource Content)> GetMultiLoadContentAsync(HttpContext context)
    {
        var languages = Split(GetValue(context, "lng"));
        var namespaces = Split(GetValue(context, "ns"));

        if (languages.Length == 0 || namespaces.Length == 0)
            return (StatusCodes.Status404NotFound, null);

        if (languages.Length * namespaces.Length > MaxMultiLoadResources)
            return (StatusCodes.Status400BadRequest, null);

        var resources = new List<(string Language, string Namespace, byte[] Json)>();

        foreach (var requestedLanguage in languages)
        {
            var language = _filter.GetLanguage(requestedLanguage);

            if (language == null)
                continue;

            foreach (var requestedNamespace in namespaces)
            {
                var ns = _filter.GetNamespace(requestedNamespace);
                var resource = ns == null ? null : await GetResourceAsync(language, ns);

                if (resource != null)
                    resources.Add((requestedLanguage, ns, resource.Json));
            }
        }

        if (resources.Count == 0)
            return (StatusCodes.Status404NotFound, null);

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream, TranslationJsonWriter.WriterOptions))
        {
            writer.WriteStartObject();

            foreach (var group in resources.GroupBy(r => r.Language))
            {
                writer.WritePropertyName(group.Key);
                writer.WriteStartObject();

                foreach (var resource in group)
                {
                    writer.WritePropertyName(resource.Namespace);
                    writer.WriteRawValue(resource.Json, true);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        return (StatusCodes.Status200OK, CachedResource.Create(stream.ToArray(), DateTimeOffset.MaxValue));
    }

    private async Task<CachedResource> GetResourceAsync(string language, string ns)
    {
        var cacheKey = (language, ns);

        if (_cache.TryGetValue(cacheKey, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
            return cached;

        var version = Volatile.Read(ref _version);
        var tree = await _backend.LoadNamespaceAsync(language, ns);

        if (tree == null)
            return null;

        var expiration = (_backend as IExpiringTranslationBackend)?.CacheExpiration;
        var resource = CachedResource.Create(TranslationJsonWriter.Write(tree.GetAllValues()),
            expiration.HasValue ? DateTimeOffset.UtcNow + expiration.Value : DateTimeOffset.MaxValue);

        if (version == Volatile.Read(ref _version) && (_cache.Count < MaxCachedResources || _cache.ContainsKey(cacheKey)))
            _cache[cacheKey] = resource;

        return resource;
    }

    private void OnTranslationsChanged(object sender, TranslationsChangedEventArgs e)
    {
        Interlocked.Increment(ref _version);

        foreach (var cacheKey in _cache.Keys)
        {
            if (e.Affects(cacheKey.Language, cacheKey.Namespace))
                _cache.TryRemove(cacheKey, out _);
        }
    }

    private static string GetValue(HttpContext context, string name)
    {
        if (context.Request.RouteValues.TryGetValue(name, out var routeValue))
            return routeValue as string;

        var values = context.Request.Query[name];

        return values.Count == 1 ? values[0] : null;
    }

    private static string[] Split(string value)
    {
        return value == null ? [] : value.Split(MultiLoadSeparators, StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal).ToArray();
    }

    private static bool IsNotModified(HttpRequest request, EntityTagHeaderValue eTag)
    {
        var ifNoneMatch = request.GetTypedHeaders().IfNoneMatch;

        return ifNoneMatch != null && ifNoneMatch.Any(t => t.Equals(EntityTagHeaderValue.Any) || t.Compare(eTag, false));
    }

    private sealed class CachedResource(byte[] json, string eTag, DateTimeOffset expiresAt)
    {
        public byte[] Json { get; } = json;

        public string ETag { get; } = eTag;

        public DateTimeOffset ExpiresAt { get; } = expiresAt;

        public static CachedResource Create(byte[] json, DateTimeOffset expiresAt)
        {
            var hash = SHA256.HashData(json);

            return new CachedResource(json, "\"" + Convert.ToHexString(hash, 0, 16).ToLowerInvariant() + "\"", expiresAt);
        }
    }
}
#endif
