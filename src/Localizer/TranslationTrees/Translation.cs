namespace Localizer.TranslationTrees;

public class Translation : TranslationTreeNode
{
    public Translation(string key, string value)
        : base(key)
    {
        Value = value;
    }

    public string Value { get; set; }
}