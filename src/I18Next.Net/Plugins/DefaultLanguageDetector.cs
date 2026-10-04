namespace I18Next.Net.Plugins;

public class DefaultLanguageDetector(string language) : ILanguageDetector
{
    private readonly string _language = language;

    public string GetLanguage()
    {
        return _language;
    }
}
