namespace Localizer.Plugins;

/// <summary>
///     Abstraction of a plural resolver plugin for I18Next.
/// </summary>
public interface IPluralResolver
{
    /// <summary>
    ///     Get the suffix which should be appended to a searched key and used to retrieve a translated value.
    /// </summary>
    /// <param name="language">The target language.</param>
    /// <param name="count">Count if items.</param>
    /// <returns>Suffix to be used to look for plural handling.</returns>
    string GetPluralSuffix(string language, int count);

    /// <summary>
    ///     Checks whether a given language needs plural handling at all.
    /// </summary>
    /// <param name="language">The target language.</param>
    /// <returns>Whether the target language requires plural handling.</returns>
    bool NeedsPlural(string language);
}
