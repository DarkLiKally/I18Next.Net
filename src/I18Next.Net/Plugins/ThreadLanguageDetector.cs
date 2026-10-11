using System.Threading;

namespace I18Next.Net.Plugins;

/// <summary>
///     A language detector which detects the name of the <see cref="System.Globalization.CultureInfo.CurrentCulture" /> of
///     the current thread, e.g. <c>de-DE</c>.
/// </summary>
public class ThreadLanguageDetector : ILanguageDetector
{
    /// <summary>
    ///     Creates a detector falling back to <c>en-US</c> for the invariant culture.
    /// </summary>
    public ThreadLanguageDetector()
    {
        FallbackLanguage = "en-US";
    }

    /// <summary>
    ///     Creates a detector falling back to the given language for the invariant culture.
    /// </summary>
    /// <param name="fallbackLanguage">The language detected when the current culture is the invariant culture.</param>
    public ThreadLanguageDetector(string fallbackLanguage)
    {
        FallbackLanguage = fallbackLanguage;
    }

    /// <summary>
    ///     The language detected when the current culture is the invariant culture, whose name is empty.
    /// </summary>
    public string FallbackLanguage { get; set; }

    /// <inheritdoc />
    public string GetLanguage()
    {
        var languageTag = Thread.CurrentThread.CurrentCulture.Name;

        return string.IsNullOrEmpty(languageTag) ? FallbackLanguage : languageTag;
    }
}
