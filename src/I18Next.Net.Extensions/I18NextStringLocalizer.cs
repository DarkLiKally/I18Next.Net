using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.Extensions.Localization;

namespace I18Next.Net.Extensions;

/// <summary>
///     An <see cref="IStringLocalizer" /> translating the names as keys with an <see cref="II18Next" /> instance, so
///     i18next translations can be used with <c>IStringLocalizer</c> and <c>IStringLocalizer&lt;T&gt;</c>.
/// </summary>
public class I18NextStringLocalizer : IStringLocalizer
{
    private readonly string _defaultNamespace;
    private readonly II18Next _instance;

    private string _language;

    /// <summary>
    ///     Creates a localizer translating in the current default namespace of the instance.
    /// </summary>
    /// <param name="instance">The instance used to translate.</param>
    public I18NextStringLocalizer(II18Next instance)
    {
        _instance = instance;

        _defaultNamespace = instance.DefaultNamespace;
    }

    /// <summary>
    ///     Creates a localizer translating in the given namespace.
    /// </summary>
    /// <param name="instance">The instance used to translate.</param>
    /// <param name="defaultNamespace">The namespace used for names without namespace prefix.</param>
    public I18NextStringLocalizer(I18NextNet instance, string defaultNamespace)
    {
        _instance = instance;

        _defaultNamespace = defaultNamespace;
    }

    /// <summary>
    ///     Gets all translations of the default namespace in the current language as stored in the backend, without
    ///     interpolation and fallbacks.
    /// </summary>
    /// <param name="includeParentCultures">
    ///     Ignored, the backend decides whether to fall back to the language part.
    /// </param>
    /// <returns>The translations keyed by their full dotted key, or no strings if the namespace does not exist.</returns>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var language = GetLanguage();

        var result = _instance.Backend.LoadNamespaceAsync(language, _defaultNamespace)
            .ConfigureAwait(false).GetAwaiter().GetResult();

        return result == null ? [] : result.GetAllValues().Select(t => new LocalizedString(t.Key, t.Value));
    }

    /// <summary>
    ///     Translates a key into the current language.
    /// </summary>
    /// <param name="name">The key to translate, optionally prefixed with its namespace, e.g. <c>common:save</c>.</param>
    /// <returns>
    ///     The translation, with <see cref="LocalizedString.ResourceNotFound" /> set when the key itself was returned.
    /// </returns>
    public LocalizedString this[string name] => Translate(name);

    /// <summary>
    ///     Translates a key into the current language with arguments.
    /// </summary>
    /// <param name="name">The key to translate, optionally prefixed with its namespace, e.g. <c>common:save</c>.</param>
    /// <param name="arguments">
    ///     Only the first argument is used as the i18next arguments, e.g. an anonymous object like
    ///     <c>new { count = 2 }</c> or a dictionary. Further arguments are ignored.
    /// </param>
    /// <returns>
    ///     The translation, with <see cref="LocalizedString.ResourceNotFound" /> set when the key itself was returned.
    /// </returns>
    public LocalizedString this[string name, params object[] arguments] => Translate(name, arguments);

    /// <summary>
    ///     Sets the language of this localizer to the given culture, overriding the language of the instance and the
    ///     language detection. Changes this localizer instead of creating a new one.
    /// </summary>
    /// <param name="culture">The culture whose IETF language tag is used as language.</param>
    /// <returns>This localizer.</returns>
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
        var value = _instance.T(language, _defaultNamespace, name, args);

        var namespaceSeparatorIndex = name.IndexOf(':');
        var key = namespaceSeparatorIndex > -1 ? name.Substring(namespaceSeparatorIndex + 1) : name;

        return new LocalizedString(name, value, value == key);
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
