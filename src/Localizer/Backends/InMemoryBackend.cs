using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Localizer.TranslationTrees;

namespace Localizer.Backends;

public class InMemoryBackend : ITranslationBackend
{
    private readonly Dictionary<string, DictionaryTranslationTree> _namespaces = new();

    public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        var treeKey = language + "_" + @namespace;

        if (_namespaces.TryGetValue(treeKey, out var tree))
            return Task.FromResult(tree as ITranslationTree);

        treeKey = BackendUtilities.GetLanguagePart(language) + "_" + @namespace;

        if (!_namespaces.TryGetValue(treeKey, out tree))
            return Task.FromResult(default(ITranslationTree));

        return Task.FromResult(tree as ITranslationTree);
    }

    public void AddTranslation(string language, string @namespace, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language cannot be null, empty or whitespace string.", nameof(language));
        if (string.IsNullOrWhiteSpace(@namespace))
            throw new ArgumentException("Namespace cannot be null, empty or whitespace string.", nameof(@namespace));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null, empty or whitespace string.", nameof(key));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var treeKey = language + "_" + @namespace;

        if (!_namespaces.TryGetValue(treeKey, out var nsDict))
        {
            nsDict = new DictionaryTranslationTree(@namespace);
            _namespaces.Add(treeKey, nsDict);
        }

        nsDict[key] = value;
    }
}