using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Combines several backends. The first backend providing a namespace wins.
/// </summary>
public class ChainedBackend : IExpiringTranslationBackend, INotifyingTranslationBackend
{
    private readonly ITranslationBackend[] _backends;
    private readonly ConcurrentDictionary<(string Language, string Namespace), CacheEntry> _cache = new();

    public ChainedBackend(params ITranslationBackend[] backends)
    {
        _backends = backends ?? throw new ArgumentNullException(nameof(backends));

        foreach (var backend in _backends)
        {
            if (backend is INotifyingTranslationBackend notifyingBackend)
                notifyingBackend.TranslationsChanged += OnTranslationsChanged;
        }
    }

    public event EventHandler<TranslationsChangedEventArgs> TranslationsChanged;

    public IReadOnlyList<ITranslationBackend> Backends => _backends;

    /// <summary>
    ///     Keeps loaded namespaces in memory so the chained backends are only asked again after <see cref="CacheExpiration" />.
    /// </summary>
    public bool CacheEnabled { get; set; }

    /// <summary>
    ///     The time after which a loaded namespace is loaded again. <c>null</c> keeps loaded namespaces forever.
    /// </summary>
    public TimeSpan? CacheExpiration { get; set; }

    /// <summary>
    ///     Returns the expired cached namespace when reloading it fails or no backend provides it anymore.
    /// </summary>
    public bool UseExpiredCacheOnFailure { get; set; } = true;

    protected virtual DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public async Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        if (!CacheEnabled)
            return await LoadFromBackendsAsync(language, @namespace).ConfigureAwait(false);

        var cacheKey = (language, @namespace);

        if (_cache.TryGetValue(cacheKey, out var entry) && entry.ExpiresAt > UtcNow)
            return entry.Tree;

        ITranslationTree tree;

        try
        {
            tree = await LoadFromBackendsAsync(language, @namespace).ConfigureAwait(false);
        }
        catch when (entry != null && UseExpiredCacheOnFailure)
        {
            return entry.Tree;
        }

        if (tree == null)
            return entry != null && UseExpiredCacheOnFailure ? entry.Tree : null;

        _cache[cacheKey] = new CacheEntry(tree, CacheExpiration.HasValue ? UtcNow + CacheExpiration.Value : DateTimeOffset.MaxValue);

        return tree;
    }

    public void ClearCache()
    {
        _cache.Clear();
    }

    public void ClearCache(string language, string @namespace)
    {
        _cache.TryRemove((language, @namespace), out _);
    }

    private void OnTranslationsChanged(object sender, TranslationsChangedEventArgs e)
    {
        foreach (var cacheKey in _cache.Keys)
        {
            if (e.Affects(cacheKey.Language, cacheKey.Namespace))
                _cache.TryRemove(cacheKey, out _);
        }

        TranslationsChanged?.Invoke(this, e);
    }

    private async Task<ITranslationTree> LoadFromBackendsAsync(string language, string @namespace)
    {
        foreach (var backend in _backends)
        {
            var tree = await backend.LoadNamespaceAsync(language, @namespace).ConfigureAwait(false);

            if (tree != null)
                return tree;
        }

        return null;
    }

    private sealed class CacheEntry(ITranslationTree tree, DateTimeOffset expiresAt)
    {
        public DateTimeOffset ExpiresAt { get; } = expiresAt;

        public ITranslationTree Tree { get; } = tree;
    }
}
