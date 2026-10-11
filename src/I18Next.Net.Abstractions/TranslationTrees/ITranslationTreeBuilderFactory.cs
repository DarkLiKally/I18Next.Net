namespace I18Next.Net.TranslationTrees;

/// <summary>
///     Creates the <see cref="ITranslationTreeBuilder" /> a backend uses for each loaded namespace, which lets backends
///     be configured with hierarchical or flat translation trees.
/// </summary>
public interface ITranslationTreeBuilderFactory
{
    /// <summary>
    ///     Creates a new, empty translation tree builder.
    /// </summary>
    /// <returns>The new builder.</returns>
    ITranslationTreeBuilder Create();
}
