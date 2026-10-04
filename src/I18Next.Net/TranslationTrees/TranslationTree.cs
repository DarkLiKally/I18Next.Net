using System.Collections.Generic;

namespace I18Next.Net.TranslationTrees;

public class TranslationTree : IHierarchicalTranslationTree
{
    private TranslationTreeNode _root;
    private Dictionary<string, Translation> _translations;

    public TranslationTree(TranslationGroup rootNode)
    {
        Root = rootNode;
    }

    public TranslationTreeNode Root
    {
        get => _root;
        set
        {
            _root = value;
            _translations = BuildTranslationIndex(value);
        }
    }

    public IDictionary<string, string> GetAllValues()
    {
        var result = new Dictionary<string, string>(_translations.Count);

        foreach (var translation in _translations)
            result.Add(translation.Key, translation.Value.Value);

        return result;
    }

    public string GetValue(string key, IDictionary<string, object> args)
    {
        if (_translations.TryGetValue(key, out var indexedTranslation))
            return indexedTranslation.Value;

        var parts = key.Split('.');

        var node = Root;

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            if (node is TranslationGroup group)
            {
                if (group.TryGetChild(part, out var foundNode))
                    node = foundNode;
                else
                    return null;

                continue;
            }

            throw new TranslationKeyInvalidException(key,
                $"The key `{key}` ends up in a final translation at part `{part}`. Cannot go down further the translation tree. Please check the key you've provided.");
        }

        if (node is TranslationGroup)
            throw new TranslationKeyInvalidException(key,
                $"The key `{key}` leads to a group of translations. Unable to resolve a final value for the given key. Please check the key you've provided.");

        var translation = (Translation) node;

        return translation.Value;
    }

    public IDictionary<string, string> GetGroupValues(string key)
    {
        var node = Root;

        if (!string.IsNullOrEmpty(key))
        {
            foreach (var part in key.Split('.'))
            {
                if (node is not TranslationGroup group || !group.TryGetChild(part, out node))
                    return null;
            }
        }

        if (node is not TranslationGroup targetGroup)
            return null;

        var result = new Dictionary<string, string>();

        MapGroupValues(result, null, targetGroup);

        return result;
    }

    public string Namespace { get; set; }

    private static Dictionary<string, Translation> BuildTranslationIndex(TranslationTreeNode root)
    {
        var result = new Dictionary<string, Translation>();

        if (root is TranslationGroup group)
            MapTranslationGroup(result, null, group);

        return result;
    }

    private static void MapGroupValues(IDictionary<string, string> result, string path, TranslationGroup group)
    {
        foreach (var node in group.Children)
        {
            var key = path == null ? node.Name : path + "." + node.Name;

            if (node is TranslationGroup subGroup)
                MapGroupValues(result, key, subGroup);
            else if (node is Translation translation && !result.ContainsKey(key))
                result.Add(key, translation.Value);
        }
    }

    private static void MapTranslationGroup(IDictionary<string, Translation> result, string path, TranslationGroup group)
    {
        foreach (var node in group.Children)
        {
            var key = path == null ? node.Name : path + "." + node.Name;

            if (node is TranslationGroup subGroup)
                MapTranslationGroup(result, key, subGroup);
            else if (node is Translation translation && !result.ContainsKey(key))
                result.Add(key, translation);
        }
    }
}