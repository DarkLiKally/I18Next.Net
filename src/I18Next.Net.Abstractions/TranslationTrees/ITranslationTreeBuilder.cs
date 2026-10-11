namespace I18Next.Net.TranslationTrees;

/// <summary>
///     Collects the translations read by a backend and builds an <see cref="ITranslationTree" /> from them. The builder
///     decides how keys are stored, e.g. split at the dots into nested groups or kept as flat keys.
/// </summary>
public interface ITranslationTreeBuilder
{
    /// <summary>
    ///     The namespace assigned to the built tree.
    /// </summary>
    string Namespace { get; set; }

    /// <summary>
    ///     Adds a translation.
    /// </summary>
    /// <param name="key">The full dotted key of the translation, e.g. <c>menu.file.open</c>.</param>
    /// <param name="text">The translated text.</param>
    void AddTranslation(string key, string text);

    /// <summary>
    ///     Builds the tree from the added translations.
    /// </summary>
    /// <returns>The translation tree with <see cref="Namespace" /> as its namespace.</returns>
    ITranslationTree Build();
}
