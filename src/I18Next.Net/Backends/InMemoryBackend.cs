using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Keeps translations in memory, e.g. for tests, translations created in code or as a cache in front of another
///     backend in a <see cref="ChainedBackend" />. Thread-safe.
/// </summary>
/// <remarks>
///     A regional language like <c>de-CH</c> falls back to the translations of its language part (<c>de</c>) when it
///     has no translations of its own.
/// </remarks>
/// <example>
///     <code>
///     var backend = new InMemoryBackend();
///     backend.AddTranslation("en", "translation", "greeting", "Hello {{name}}!");
///     backend.AddTranslation("de", "translation", "greeting", "Hallo {{name}}!");
///     </code>
/// </example>
public class InMemoryBackend : IWritableTranslationBackend
{
    private readonly ConcurrentDictionary<string, DictionaryTranslationTree> _namespaces = new();

    /// <inheritdoc />
    /// <remarks>Returns null if neither the language nor its language part have translations for the namespace.</remarks>
    public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        var treeKey = language + "_" + @namespace;

        if (_namespaces.TryGetValue(treeKey, out var tree))
            return Task.FromResult(tree as ITranslationTree);

        treeKey = BackendUtilities.GetLanguagePart(language) + "_" + @namespace;

        return !_namespaces.TryGetValue(treeKey, out tree)
            ? Task.FromResult(default(ITranslationTree))
            : Task.FromResult(tree as ITranslationTree);
    }

    /// <summary>
    ///     Replaces the translations of a namespace with a copy of the translations of the given tree.
    /// </summary>
    /// <param name="language">The language of the namespace.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="tree">The translation tree whose translations are copied.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="ArgumentException">
    ///     <paramref name="language" /> or <paramref name="namespace" /> is null, empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="tree" /> is null.</exception>
    public Task SaveNamespaceAsync(string language, string @namespace, ITranslationTree tree)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be null, empty or whitespace string.", nameof(language));
        if (string.IsNullOrWhiteSpace(@namespace))
            throw new ArgumentException("Namespace cannot be null, empty or whitespace string.", nameof(@namespace));
        if (tree == null)
            throw new ArgumentNullException(nameof(tree));

        _namespaces[language + "_" + @namespace] = new DictionaryTranslationTree(@namespace, tree.GetAllValues());

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Adds a translation or replaces the existing translation with the same key.
    /// </summary>
    /// <param name="language">The language, e.g. <c>de-DE</c>.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="key">The full key, e.g. <c>menu.file.open</c>.</param>
    /// <param name="value">The translation.</param>
    /// <exception cref="ArgumentException">
    ///     <paramref name="language" />, <paramref name="namespace" /> or <paramref name="key" /> is null, empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is null.</exception>
    public void AddTranslation(string language, string @namespace, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be null, empty or whitespace string.", nameof(language));
        if (string.IsNullOrWhiteSpace(@namespace))
            throw new ArgumentException("Namespace cannot be null, empty or whitespace string.", nameof(@namespace));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null, empty or whitespace string.", nameof(key));
        var treeKey = language + "_" + @namespace;

        if (!_namespaces.TryGetValue(treeKey, out var nsDict))
            nsDict = _namespaces.GetOrAdd(treeKey, new DictionaryTranslationTree(@namespace));

        nsDict[key] = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     Adds translations or replaces the existing translations with the same keys.
    /// </summary>
    /// <param name="language">The language, e.g. <c>de-DE</c>.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="translations">The translations keyed by their full keys, e.g. <c>menu.file.open</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="translations" /> or one of its values is null.</exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="language" />, <paramref name="namespace" /> or a key is null, empty or whitespace.
    /// </exception>
    public void AddTranslations(string language, string @namespace, IDictionary<string, string> translations)
    {
        if (translations == null)
            throw new ArgumentNullException(nameof(translations));

        foreach (var translation in translations)
            AddTranslation(language, @namespace, translation.Key, translation.Value);
    }

    /// <summary>
    ///     Checks whether the backend has translations of a namespace for exactly the given language, without falling back
    ///     to the language part.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>Whether the namespace exists for the language.</returns>
    public bool HasNamespace(string language, string @namespace)
    {
        return _namespaces.ContainsKey(language + "_" + @namespace);
    }

    /// <summary>
    ///     Removes all translations of a namespace in the given language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <param name="namespace">The namespace.</param>
    /// <returns>Whether the namespace existed.</returns>
    public bool RemoveNamespace(string language, string @namespace)
    {
        return _namespaces.TryRemove(language + "_" + @namespace, out _);
    }
}
