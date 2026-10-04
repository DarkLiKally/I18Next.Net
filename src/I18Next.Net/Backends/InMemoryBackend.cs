using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

public class InMemoryBackend : ITranslationBackend
{
    private readonly Dictionary<string, DictionaryTranslationTree> _namespaces = [];

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
        {
            nsDict = new DictionaryTranslationTree(@namespace);
            _namespaces.Add(treeKey, nsDict);
        }

        nsDict[key] = value ?? throw new ArgumentNullException(nameof(value));
    }

    public void AddTranslations(string language, string @namespace, IDictionary<string, string> translations)
    {
        if (translations == null)
            throw new ArgumentNullException(nameof(translations));

        foreach (var translation in translations)
            AddTranslation(language, @namespace, translation.Key, translation.Value);
    }

    public bool HasNamespace(string language, string @namespace)
    {
        return _namespaces.ContainsKey(language + "_" + @namespace);
    }

    public bool RemoveNamespace(string language, string @namespace)
    {
        return _namespaces.Remove(language + "_" + @namespace);
    }
}
