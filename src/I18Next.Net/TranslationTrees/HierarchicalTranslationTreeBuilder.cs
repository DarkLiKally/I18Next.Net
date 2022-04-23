using System.Collections.Generic;

namespace I18Next.Net.TranslationTrees;

public class HierarchicalTranslationTreeBuilder : ITranslationTreeBuilder
{
    private readonly Dictionary<string, Dictionary<string, object>> _groups = new Dictionary<string, Dictionary<string, object>>();

    private readonly Dictionary<string, object> _root = new Dictionary<string, object>();

    public void AddTranslation(string key, string text)
    {
        var parts = key.Split('.');

        var parentGroup = _root;

        if (parts.Length > 1)
        {
            var currentPath = "";

            for (var i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i];

                if (i > 0)
                    currentPath += ".";
                currentPath += part;

                if (_groups.ContainsKey(currentPath))
                {
                    parentGroup = _groups[currentPath];
                }
                else
                {
                    var group = new Dictionary<string, object>();
                    parentGroup.Add(part, group);
                    parentGroup = group;
                    _groups.Add(currentPath, group);
                }
            }
        }

        parentGroup.Add(parts[parts.Length - 1], text);
    }

    public ITranslationTree Build()
    {
        var root = BuildNode("", _root);

        return new TranslationTree(root);
    }

    public string Namespace { get; set; }

    private TranslationGroup BuildNode(string name, Dictionary<string, object> parentNode)
    {
        var nodes = new List<TranslationTreeNode>();

        foreach (var node in parentNode)
        {
            if (node.Value is Dictionary<string, object> childGroup)
            {
                var group = BuildNode(node.Key, childGroup);

                nodes.Add(group);
            }

            else
            {
                var entry = new Translation(node.Key, node.Value as string);

                nodes.Add(entry);
            }
        }

        return new TranslationGroup(name, nodes.ToArray());
    }
}