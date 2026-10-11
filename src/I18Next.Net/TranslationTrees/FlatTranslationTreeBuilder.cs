namespace I18Next.Net.TranslationTrees;

/// <summary>
///     Builds translation trees which use the full keys without splitting them into nested groups. This is the equivalent of
///     disabling the key separator in i18next, allowing keys like "Hello. How are you?".
/// </summary>
public class FlatTranslationTreeBuilder : ITranslationTreeBuilder
{
    private readonly DictionaryTranslationTree _tree = new(null);

    /// <inheritdoc />
    public string Namespace { get; set; }

    /// <inheritdoc />
    /// <remarks>A translation with the same key replaces the existing one.</remarks>
    public void AddTranslation(string key, string text)
    {
        _tree.AddValue(key, text);
    }

    /// <inheritdoc />
    /// <remarks>Returns a <see cref="DictionaryTranslationTree" />.</remarks>
    public ITranslationTree Build()
    {
        _tree.Namespace = Namespace;

        return _tree;
    }
}
