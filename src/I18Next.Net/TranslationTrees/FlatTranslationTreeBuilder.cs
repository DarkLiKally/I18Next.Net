namespace I18Next.Net.TranslationTrees;

/// <summary>
///     Builds translation trees which use the full keys without splitting them into nested groups. This is the equivalent of
///     disabling the key separator in i18next, allowing keys like "Hello. How are you?".
/// </summary>
public class FlatTranslationTreeBuilder : ITranslationTreeBuilder
{
    private readonly DictionaryTranslationTree _tree = new(null);

    public string Namespace { get; set; }

    public void AddTranslation(string key, string text)
    {
        _tree.AddValue(key, text);
    }

    public ITranslationTree Build()
    {
        _tree.Namespace = Namespace;

        return _tree;
    }
}
