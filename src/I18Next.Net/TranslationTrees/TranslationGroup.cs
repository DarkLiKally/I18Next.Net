using System.Collections.Generic;

namespace I18Next.Net.TranslationTrees;

/// <summary>
///     A node of a <see cref="TranslationTree" /> grouping translations and nested groups, e.g. <c>menu</c> for
///     <c>menu.file.open</c>.
/// </summary>
public class TranslationGroup : TranslationTreeNode
{
    private readonly Dictionary<string, TranslationTreeNode> _childrenByName;

    /// <summary>
    ///     Creates a group with the given children.
    /// </summary>
    /// <param name="name">The name of the group within its parent group; empty for the root group.</param>
    /// <param name="children">
    ///     The translations and groups of the group. Only the first child of a name can be looked up.
    /// </param>
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

    /// <summary>
    ///     The translations and groups of the group.
    /// </summary>
    public TranslationTreeNode[] Children { get; }

    /// <summary>
    ///     Looks up a child by its name.
    /// </summary>
    /// <param name="name">The name of the child, e.g. <c>file</c>.</param>
    /// <param name="child">The child, or null if the group has no child of that name.</param>
    /// <returns>Whether the group has a child of that name.</returns>
    public bool TryGetChild(string name, out TranslationTreeNode child)
    {
        return _childrenByName.TryGetValue(name, out child);
    }
}
