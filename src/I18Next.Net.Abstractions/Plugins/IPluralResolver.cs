namespace I18Next.Net.Plugins;

/// <summary>
///     A plural resolver plugin which determines the plural keys the translator looks for when a translation is requested
///     with a numeric <c>count</c> argument.
/// </summary>
public interface IPluralResolver
{
    /// <summary>
    ///     Gets the suffix appended to the key to find the plural form for a count, including the separator, e.g.
    ///     <c>_one</c> or <c>_other</c> in the i18next v4 JSON format and an empty string or <c>_plural</c> in the v3
    ///     format. The translator falls back to the key without suffix when the suffixed key is missing.
    /// </summary>
    /// <param name="language">The target language.</param>
    /// <param name="count">Count of items.</param>
    /// <returns>The suffix or an empty string to use the key without suffix.</returns>
    string GetPluralSuffix(string language, int count);

    /// <summary>
    ///     Checks whether a given language needs plural handling at all. When it returns <c>false</c>, the translator
    ///     ignores the <c>count</c> argument for the key lookup.
    /// </summary>
    /// <param name="language">The target language.</param>
    /// <returns>Whether the target language requires plural handling.</returns>
    bool NeedsPlural(string language);
}
