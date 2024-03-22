using System.Threading;

namespace Localizer.Plugins;

public class ThreadLanguageDetector : ILanguageDetector
{
    public ThreadLanguageDetector()
    {
        FallbackLanguage = "en-US";
    }

    public ThreadLanguageDetector(string fallbackLanguage)
    {
        FallbackLanguage = fallbackLanguage;
    }

    public string FallbackLanguage { get; set; }

    public string GetLanguage()
    {
        var languageTag = Thread.CurrentThread.CurrentCulture.Name;

        if (string.IsNullOrEmpty(languageTag))
            return FallbackLanguage;

        return languageTag;
    }
}