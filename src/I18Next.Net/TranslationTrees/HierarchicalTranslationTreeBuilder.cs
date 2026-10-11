using System.Collections.Generic;

namespace I18Next.Net.TranslationTrees;

/// <summary>
///     Builds a <see cref="TranslationTree" /> which splits the keys at dots into nested groups, like the default
///     <c>keySeparator</c> of i18next. The default builder of the backends.
/// </summary>
public class HierarchicalTranslationTreeBuilder : ITranslationTreeBuilder
{
    private readonly Dictionary<string, Dictionary<string, object>> _groups = [];

    private readonly Dictionary<string, object> _root = [];

    /// <inheritdoc />
    /// <exception cref="System.ArgumentException">
    ///     The key was already added, or it conflicts with an added key, e.g. <c>a.b</c> and <c>a</c>.
    /// </exception>
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

    /// <inheritdoc />
    public ITranslationTree Build()
    {
        var root = BuildNode("", _root);

        return new TranslationTree(root) { Namespace = Namespace };
    }

    /// <inheritdoc />
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

        return new TranslationGroup(name, [.. nodes]);
    }
}
