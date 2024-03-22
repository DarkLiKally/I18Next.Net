namespace Localizer.Plugins;

public class DefaultLanguageDetector : ILanguageDetector
{
    private readonly string _language;

    public DefaultLanguageDetector(string language)
    {
        _language = language;
    }

    public string GetLanguage()
    {
        return _language;
    }
}