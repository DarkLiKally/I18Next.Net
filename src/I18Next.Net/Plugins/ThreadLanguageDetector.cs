using System.Threading;

namespace I18Next.Net.Plugins;

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

        return string.IsNullOrEmpty(languageTag) ? FallbackLanguage : languageTag;
    }
}
