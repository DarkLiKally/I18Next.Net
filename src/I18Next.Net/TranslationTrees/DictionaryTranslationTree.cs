using System;
using System.Collections.Generic;

namespace I18Next.Net.TranslationTrees;

public class DictionaryTranslationTree : IHierarchicalTranslationTree
{
    private readonly Dictionary<string, string> _dictionary;

    public DictionaryTranslationTree(string @namespace)
    {
        Namespace = @namespace;
        _dictionary = new Dictionary<string, string>();
    }

    public DictionaryTranslationTree(string @namespace, IDictionary<string, string> translations)
    {
        Namespace = @namespace;
        _dictionary = new Dictionary<string, string>(translations);
    }

    public string this[string key]
    {
        get => _dictionary[key];
        set => _dictionary[key] = value;
    }

    public IDictionary<string, string> GetAllValues()
    {
        return new Dictionary<string, string>(_dictionary);
    }

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

            result ??= new Dictionary<string, string>();
            result[entry.Key.Substring(prefix.Length)] = entry.Value;
        }

        return result;
    }

    public string GetValue(string key, IDictionary<string, object> args)
    {
        return !_dictionary.TryGetValue(key, out var value) ? null : value;
    }

    public string Namespace { get; set; }

    public void AddValue(string key, string value)
    {
        _dictionary[key] = value;
    }
}