namespace I18Next.Net.TranslationTrees;

/// <summary>
///     The base class of the nodes of a <see cref="TranslationTree" />.
/// </summary>
/// <param name="name">The name of the node within its parent group.</param>
public abstract class TranslationTreeNode(string name)
{
    /// <summary>
    ///     The name of the node within its parent group, e.g. <c>file</c> for the group <c>menu.file</c>.
    /// </summary>
    public string Name { get; set; } = name;
}
