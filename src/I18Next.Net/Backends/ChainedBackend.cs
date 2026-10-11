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

    /// <summary>
    ///     Creates a backend asking the given backends in order, e.g. an <see cref="InMemoryBackend" /> cache in front of an
    ///     <see cref="HttpBackend" />. Changes notified by backends implementing <see cref="INotifyingTranslationBackend" />
    ///     remove the affected namespaces from the cache and are forwarded through <see cref="TranslationsChanged" />.
    /// </summary>
    /// <param name="backends">The backends in order of precedence.</param>
    /// <exception cref="ArgumentNullException"><paramref name="backends" /> is null.</exception>
    public ChainedBackend(params ITranslationBackend[] backends)
    {
        _backends = backends ?? throw new ArgumentNullException(nameof(backends));

        foreach (var backend in _backends)
        {
            if (backend is INotifyingTranslationBackend notifyingBackend)
                notifyingBackend.TranslationsChanged += OnTranslationsChanged;
        }
    }

    /// <inheritdoc />
    public event EventHandler<TranslationsChangedEventArgs> TranslationsChanged;

    /// <summary>
    ///     The chained backends in order of precedence.
    /// </summary>
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

    /// <summary>
    ///     Stores a namespace provided by a later backend in all earlier backends implementing
    ///     <see cref="IWritableTranslationBackend" />, like the i18next-chained-backend does, e.g. to fill a distributed cache
    ///     in front of a HTTP backend. Disabled by default because the earlier backends provide the stored namespaces from
    ///     then on, so changes of the later backends are only picked up when the stored namespaces expire.
    /// </summary>
    public bool SaveToEarlierBackends { get; set; }

    /// <summary>
    ///     The current time used to expire cached namespaces. Can be overridden to control the time in tests.
    /// </summary>
    protected virtual DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    /// <remarks>
    ///     Returns the namespace of the first backend providing it, or null if no backend provides it. Uses and fills the
    ///     cache when <see cref="CacheEnabled" /> is set.
    /// </remarks>
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

    /// <summary>
    ///     Removes all namespaces from the cache, so they are loaded from the backends again when they are used next.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    ///     Removes a namespace from the cache, so it is loaded from the backends again when it is used next.
    /// </summary>
    /// <param name="language">The language of the namespace.</param>
    /// <param name="namespace">The namespace.</param>
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
        for (var i = 0; i < _backends.Length; i++)
        {
            var tree = await _backends[i].LoadNamespaceAsync(language, @namespace).ConfigureAwait(false);

            if (tree == null)
                continue;

            if (SaveToEarlierBackends)
                await SaveToEarlierBackendsAsync(language, @namespace, tree, i).ConfigureAwait(false);

            return tree;
        }

        return null;
    }

    private async Task SaveToEarlierBackendsAsync(string language, string @namespace, ITranslationTree tree, int index)
    {
        for (var i = 0; i < index; i++)
        {
            if (_backends[i] is IWritableTranslationBackend writableBackend)
                await writableBackend.SaveNamespaceAsync(language, @namespace, tree).ConfigureAwait(false);
        }
    }

    private sealed class CacheEntry(ITranslationTree tree, DateTimeOffset expiresAt)
    {
        public DateTimeOffset ExpiresAt { get; } = expiresAt;

        public ITranslationTree Tree { get; } = tree;
    }
}
