namespace I18Next.Net.TranslationTrees;

public abstract class TranslationTreeNode
{
    protected TranslationTreeNode(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}