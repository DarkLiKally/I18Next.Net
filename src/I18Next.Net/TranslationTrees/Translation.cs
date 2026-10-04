namespace I18Next.Net.TranslationTrees;

public class Translation(string key, string value) : TranslationTreeNode(key)
{
    public string Value { get; set; } = value;
}
