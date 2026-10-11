namespace I18Next.Net.TranslationTrees;

/// <summary>
///     Creates translation tree builders of the given type with its parameterless constructor, e.g.
///     <c>new GenericTranslationTreeBuilderFactory&lt;FlatTranslationTreeBuilder&gt;()</c> to load keys containing dots
///     without splitting them into groups.
/// </summary>
/// <typeparam name="T">The type of the created builders.</typeparam>
public class GenericTranslationTreeBuilderFactory<T> : ITranslationTreeBuilderFactory
    where T : ITranslationTreeBuilder, new()
{
    /// <inheritdoc />
    public ITranslationTreeBuilder Create()
    {
        return new T();
    }
}
