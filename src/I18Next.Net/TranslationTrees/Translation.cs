namespace I18Next.Net.TranslationTrees;

/// <summary>
///     A leaf of a <see cref="TranslationTree" /> holding a single translation.
/// </summary>
/// <param name="key">The name of the translation within its group, e.g. <c>open</c> for <c>menu.file.open</c>.</param>
/// <param name="value">The translation.</param>
public class Translation(string key, string value) : TranslationTreeNode(key)
{
    /// <summary>
    ///     The translation.
    /// </summary>
    public string Value { get; set; } = value;
}
