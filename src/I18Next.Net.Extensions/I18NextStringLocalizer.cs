using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace I18Next.Net.Extensions;

public class I18NextStringLocalizer : IStringLocalizer
{
    private readonly string _defaultNamespace;
    private readonly II18Next _instance;

    private string _language;

    public I18NextStringLocalizer(II18Next instance)
    {
        _instance = instance;

        _defaultNamespace = instance.DefaultNamespace;
    }

    public I18NextStringLocalizer(I18NextNet instance, string defaultNamespace)
    {
        _instance = instance;

        _defaultNamespace = defaultNamespace;
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var language = GetLanguage();

        var result = _instance.Backend.LoadNamespaceAsync(language, _defaultNamespace)
            .ConfigureAwait(false).GetAwaiter().GetResult();

        if (result == null)
            return Enumerable.Empty<LocalizedString>();

        return result.GetAllValues().Select(t => new LocalizedString(t.Key, t.Value));
    }

    public LocalizedString this[string name] => Translate(name);

    public LocalizedString this[string name, params object[] arguments] => Translate(name, arguments);

    public IStringLocalizer WithCulture(CultureInfo culture)
    {
        _language = culture.IetfLanguageTag;

        return this;
    }

    private LocalizedString Translate(string name, object[] arguments = null)
    {
        object args = null;

        if (arguments != null && arguments.Length > 0)
            args = arguments[0];

        var language = GetLanguage();

        return new LocalizedString(name, _instance.T(language, _defaultNamespace, name, args));
    }

    private string GetLanguage()
    {
        if (_language != null)
            return _language;

        if (_instance.DetectLanguageOnEachTranslation && _instance.LanguageDetector != null)
        {
            var detectedLanguage = _instance.LanguageDetector.GetLanguage();

            if (!string.IsNullOrWhiteSpace(detectedLanguage))
                return detectedLanguage;
        }

        return _instance.Language;
    }
}