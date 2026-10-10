using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using FluentValidation.Resources;

namespace I18Next.Net.FluentValidation;

/// <summary>
///     Language manager of FluentValidation which resolves the message templates from I18Next. The built-in messages of
///     FluentValidation are used if a key is missing.
/// </summary>
public class I18NextLanguageManager : ILanguageManager
{
    private static readonly Regex PlaceholderRegex = new(@"\{\{-?\s*([^{}\s]+)\s*\}\}", RegexOptions.Compiled);

    private readonly LanguageManager _fallbackLanguageManager = new();
    private readonly II18Next _i18Next;

    /// <summary>
    ///     Constructor using the default options.
    /// </summary>
    /// <param name="i18Next">The I18Next instance used to translate the messages.</param>
    public I18NextLanguageManager(II18Next i18Next)
        : this(i18Next, new I18NextFluentValidationOptions())
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="i18Next">The I18Next instance used to translate the messages.</param>
    /// <param name="options">The options of the FluentValidation integration.</param>
    public I18NextLanguageManager(II18Next i18Next, I18NextFluentValidationOptions options)
    {
        _i18Next = i18Next ?? throw new ArgumentNullException(nameof(i18Next));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    ///     The options of the FluentValidation integration.
    /// </summary>
    public I18NextFluentValidationOptions Options { get; }

    /// <summary>
    ///     Whether translations are enabled. If disabled, the built-in English messages of FluentValidation are used.
    /// </summary>
    public bool Enabled
    {
        get => _fallbackLanguageManager.Enabled;
        set => _fallbackLanguageManager.Enabled = value;
    }

    /// <summary>
    ///     The culture used for all messages. Uses the current language of I18Next if not set.
    /// </summary>
    public CultureInfo Culture { get; set; }

    /// <summary>
    ///     Gets the message template of the given key. The i18next placeholders like <c>{{PropertyName}}</c> are converted to
    ///     the placeholders of FluentValidation.
    /// </summary>
    /// <param name="key">The key of the message, e.g. <c>NotEmptyValidator</c>.</param>
    /// <param name="culture">The culture of the message. Uses <see cref="Culture" /> or the current language of I18Next if not provided.</param>
    /// <returns>The message template.</returns>
    public string GetString(string key, CultureInfo culture = null)
    {
        culture ??= Culture;

        if (!Enabled || string.IsNullOrEmpty(key))
            return _fallbackLanguageManager.GetString(key, culture);

        var language = string.IsNullOrEmpty(culture?.Name) ? TranslationHelper.GetLanguage(_i18Next) : culture.Name;
        var messageKey = string.IsNullOrEmpty(Options.KeyPrefix) ? key : Options.KeyPrefix + "." + key;

        if (!TranslationHelper.Exists(_i18Next, language, TranslationHelper.QualifyKey(Options.Namespace, messageKey)))
            return _fallbackLanguageManager.GetString(key, culture ?? GetCulture(language));

        var message = _i18Next.T(language, Options.Namespace, messageKey, new Dictionary<string, object> { ["interpolate"] = false });

        return PlaceholderRegex.Replace(message, "{$1}");
    }

    private static CultureInfo GetCulture(string language)
    {
        try
        {
            return CultureInfo.GetCultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.CurrentUICulture;
        }
    }
}
