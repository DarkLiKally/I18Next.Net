namespace I18Next.Net.Plugins;

/// <summary>
///     A language detector which always detects the same language.
/// </summary>
/// <param name="language">The language returned by <see cref="GetLanguage" />.</param>
public class DefaultLanguageDetector(string language) : ILanguageDetector
{
    private readonly string _language = language;

    /// <inheritdoc />
    public string GetLanguage()
    {
        return _language;
    }
}
