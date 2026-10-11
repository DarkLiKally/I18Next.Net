using System;
using System.Collections.Generic;

namespace I18Next.Net.TranslationTrees;

/// <summary>
///     A translation tree storing the translations in a dictionary by their full keys, e.g. <c>menu.file.open</c>.
///     Groups are resolved by the key prefix, and keys are used as they are without splitting them at dots.
/// </summary>
public class DictionaryTranslationTree : IHierarchicalTranslationTree
{
    private readonly Dictionary<string, string> _dictionary;

    /// <summary>
    ///     Creates an empty tree.
    /// </summary>
    /// <param name="namespace">The namespace of the translations.</param>
    public DictionaryTranslationTree(string @namespace)
    {
        Namespace = @namespace;
        _dictionary = [];
    }

    /// <summary>
    ///     Creates a tree containing a copy of the given translations.
    /// </summary>
    /// <param name="namespace">The namespace of the translations.</param>
    /// <param name="translations">The translations keyed by their full keys.</param>
    /// <exception cref="ArgumentNullException"><paramref name="translations" /> is null.</exception>
    public DictionaryTranslationTree(string @namespace, IDictionary<string, string> translations)
    {
        Namespace = @namespace;
        _dictionary = new Dictionary<string, string>(translations);
    }

    /// <summary>
    ///     The translation with the given full key.
    /// </summary>
    /// <param name="key">The full key, e.g. <c>menu.file.open</c>.</param>
    /// <exception cref="KeyNotFoundException">The key does not exist when getting the translation.</exception>
    public string this[string key]
    {
        get => _dictionary[key];
        set => _dictionary[key] = value;
    }

    /// <inheritdoc />
    public IDictionary<string, string> GetAllValues()
    {
        return new Dictionary<string, string>(_dictionary);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     A null or empty key returns all translations, or null if the tree is empty.
    /// </remarks>
    public IDictionary<string, string> GetGroupValues(string key)
    {
        if (string.IsNullOrEmpty(key))
            return _dictionary.Count == 0 ? null : GetAllValues();

        var prefix = key + ".";
        Dictionary<string, string> result = null;

        foreach (var entry in _dictionary)
        {
            if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            result ??= [];
            result[entry.Key.Substring(prefix.Length)] = entry.Value;
        }

        return result;
    }

    /// <inheritdoc />
    public string GetValue(string key, IDictionary<string, object> args)
    {
        return !_dictionary.TryGetValue(key, out var value) ? null : value;
    }

    /// <inheritdoc />
    public string Namespace { get; set; }

    /// <summary>
    ///     Adds a translation or replaces the existing translation with the same key.
    /// </summary>
    /// <param name="key">The full key, e.g. <c>menu.file.open</c>.</param>
    /// <param name="value">The translation.</param>
    public void AddValue(string key, string value)
    {
        _dictionary[key] = value;
    }
}
