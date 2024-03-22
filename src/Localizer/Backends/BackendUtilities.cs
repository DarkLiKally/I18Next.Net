namespace Localizer.Backends;

/// <summary>
///     Provides some utility methods for easier implementation of backends.
/// </summary>
public static class BackendUtilities
{
    /// <summary>
    ///     Extracts the language only part of a language string of the form "de-DE". For the provided sample the result would
    ///     be "de".
    /// </summary>
    /// <param name="language">The language input string.</param>
    /// <returns>The language part of the input string.</returns>
    public static string GetLanguagePart(string language)
    {
        var index = language.IndexOf('-');

        if (index == -1)
            return language;

        return language.Substring(0, index);
    }
}