namespace Localizer.TranslationTrees;

public class TranslationGroup : TranslationTreeNode
{
    public TranslationGroup(string name, TranslationTreeNode[] children)
        : base(name)
    {
        Children = children;
    }

    public TranslationTreeNode[] Children { get; }
}