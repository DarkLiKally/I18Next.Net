﻿using System.Collections.Generic;
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
        var language = _language ?? _instance.Language;

        var result = _instance.Backend.LoadNamespaceAsync(language, _instance.DefaultNamespace)
            .ConfigureAwait(false).GetAwaiter().GetResult();

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

        if (_instance.DetectLanguageOnEachTranslation)
            _instance.UseDetectedLanguage();

        var language = _language ?? _instance.Language;

        try
        {
            return new LocalizedString(name, _instance.T(language, _defaultNamespace, name, args));
        }
        catch (TranslationNotFoundException ex)
        {
            return new LocalizedString(name, ex.AlternateString, true);
        }
    }
}
