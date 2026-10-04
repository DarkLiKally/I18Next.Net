using System.Collections.Generic;

namespace I18Next.Net.TranslationTrees;

public class TranslationGroup : TranslationTreeNode
{
    private readonly Dictionary<string, TranslationTreeNode> _childrenByName;

    public TranslationGroup(string name, TranslationTreeNode[] children)
        : base(name)
    {
        Children = children;

        _childrenByName = new Dictionary<string, TranslationTreeNode>(children.Length);

        foreach (var child in children)
        {
            if (!_childrenByName.ContainsKey(child.Name))
                _childrenByName.Add(child.Name, child);
        }
    }

    public TranslationTreeNode[] Children { get; }

    public bool TryGetChild(string name, out TranslationTreeNode child)
    {
        return _childrenByName.TryGetValue(name, out child);
    }
}
