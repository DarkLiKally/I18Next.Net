using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using I18Next.Net.Backends;
using I18Next.Net.Logging;
using I18Next.Net.TranslationTrees;

using Microsoft.Extensions.Caching.Distributed;

namespace I18Next.Net.Extensions.Backends;

/// <summary>
///     Stores namespaces as JSON in an <see cref="IDistributedCache" />, e.g. Redis shared by several servers. Meant as the
///     first backend of a <see cref="ChainedBackend" /> with <see cref="ChainedBackend.SaveToEarlierBackends" /> enabled, so
///     the namespaces loaded from the following backends are cached. Namespaces are stored for the requested language, there
///     is no fallback to the language part.
/// </summary>
public class DistributedCacheBackend : IWritableTranslationBackend
{
    public const string DefaultKeyPrefix = "i18next:";

    private readonly IDistributedCache _cache;
    private readonly ITranslationTreeBuilderFactory _treeBuilderFactory;

    public DistributedCacheBackend(IDistributedCache cache)
        : this(cache, new GenericTranslationTreeBuilderFactory<HierarchicalTranslationTreeBuilder>())
    {
    }

    public DistributedCacheBackend(IDistributedCache cache, ITranslationTreeBuilderFactory treeBuilderFactory)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _treeBuilderFactory = treeBuilderFactory ?? throw new ArgumentNullException(nameof(treeBuilderFactory));
    }

    /// <summary>
    ///     The prefix of the cache keys, which are formed as <c>{prefix}{language}:{namespace}</c>.
    /// </summary>
    public string KeyPrefix { get; set; } = DefaultKeyPrefix;

    /// <summary>
    ///     The expiration of stored namespaces. Namespaces expire after 7 days by default like in the
    ///     i18next-localstorage-backend.
    /// </summary>
    public DistributedCacheEntryOptions EntryOptions { get; set; } = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7) };

    /// <summary>
    ///     Treats failing cache reads and unreadable cache entries as missing namespaces and ignores failing writes, so an
    ///     unavailable cache does not prevent loading the namespaces from the following backends.
    /// </summary>
    public bool IgnoreCacheFailures { get; set; } = true;

    /// <summary>
    ///     Logs ignored cache failures as warnings.
    /// </summary>
    public ILogger Logger { get; set; }

    public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        try
        {
            var data = await _cache.GetAsync(GetKey(language, @namespace)).ConfigureAwait(false);

            return data == null ? null : Read(data, @namespace);
        }
        catch (Exception ex) when (IgnoreCacheFailures)
        {
            if (Logger != null && Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning(ex, "Loading the namespace {language}.{ns} from the distributed cache failed.", language, @namespace);

            return null;
        }
    }

    public async Task SaveNamespaceAsync(string language, string @namespace, ITranslationTree tree)
    {
        if (tree == null)
            throw new ArgumentNullException(nameof(tree));

        var data = Write(tree.GetAllValues());

        try
        {
            await _cache.SetAsync(GetKey(language, @namespace), data, EntryOptions).ConfigureAwait(false);
        }
        catch (Exception ex) when (IgnoreCacheFailures)
        {
            if (Logger != null && Logger.IsEnabled(LogLevel.Warning))
                Logger.LogWarning(ex, "Saving the namespace {language}.{ns} to the distributed cache failed.", language, @namespace);
        }
    }

    /// <summary>
    ///     Removes a stored namespace, e.g. after its translations were updated. Failures are not ignored.
    /// </summary>
    public Task RemoveNamespaceAsync(string language, string @namespace)
    {
        return _cache.RemoveAsync(GetKey(language, @namespace));
    }

    protected virtual string GetKey(string language, string @namespace)
    {
        return $"{KeyPrefix}{language}:{@namespace}";
    }

    private ITranslationTree Read(byte[] data, string @namespace)
    {
        var builder = _treeBuilderFactory.Create();
        builder.Namespace = @namespace;

        using var document = JsonDocument.Parse(data);

        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
                builder.AddTranslation(property.Name, property.Value.GetString());
        }

        return builder.Build();
    }

    private static byte[] Write(IDictionary<string, string> values)
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            foreach (var value in values)
            {
                if (value.Value != null)
                    writer.WriteString(value.Key, value.Value);
            }

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }
}
