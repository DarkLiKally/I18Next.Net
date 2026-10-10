using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.DataAnnotations;

/// <summary>
///     Translates the error messages of validation attributes and display names using I18Next.
/// </summary>
public class I18NextValidationLocalizer
{
    private const string ModelBindingKeyPrefix = "modelBinding.";

    private static readonly ConcurrentDictionary<Type, string> DefaultErrorMessages = new();

    private readonly II18Next _i18Next;

    /// <summary>
    ///     Constructor using the default options.
    /// </summary>
    /// <param name="i18Next">The I18Next instance used to translate the messages.</param>
    public I18NextValidationLocalizer(II18Next i18Next)
        : this(i18Next, new I18NextDataAnnotationsOptions())
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="i18Next">The I18Next instance used to translate the messages.</param>
    /// <param name="options">The options of the DataAnnotations integration.</param>
    public I18NextValidationLocalizer(II18Next i18Next, I18NextDataAnnotationsOptions options)
    {
        _i18Next = i18Next ?? throw new ArgumentNullException(nameof(i18Next));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    ///     The options of the DataAnnotations integration.
    /// </summary>
    public I18NextDataAnnotationsOptions Options { get; }

    /// <summary>
    ///     Checks whether the error message of the given attribute is translated. Attributes using resource based messages are
    ///     never translated.
    /// </summary>
    /// <param name="attribute">The validation attribute.</param>
    /// <returns>Whether the error message of the attribute is translated.</returns>
    public bool CanTranslate(ValidationAttribute attribute)
    {
        if (attribute == null)
            throw new ArgumentNullException(nameof(attribute));

        if (attribute.ErrorMessageResourceType != null || !string.IsNullOrEmpty(attribute.ErrorMessageResourceName))
            return false;

        return HasCustomErrorMessage(attribute) ? Options.UseErrorMessagesAsKeys : Options.FindMapping(attribute.GetType()) != null;
    }

    /// <summary>
    ///     Translates a display name. The name is used as the key and returned unchanged if no translation exists.
    /// </summary>
    /// <param name="name">The display name or the name of the property.</param>
    /// <param name="language">The target language. Uses the current language of I18Next if not provided.</param>
    /// <returns>The translated display name.</returns>
    public string GetDisplayName(string name, string language = null)
    {
        if (string.IsNullOrEmpty(name) || !Options.TranslateDisplayNames)
            return name;

        language ??= GetLanguage();

        var key = QualifyKey(Options.DisplayNameNamespace, name);

        return Exists(language, key) ? _i18Next.T(language, key) : name;
    }

    /// <summary>
    ///     Gets the translated error message of a validation attribute.
    /// </summary>
    /// <param name="attribute">The validation attribute which failed.</param>
    /// <param name="displayName">The already translated display name of the validated member.</param>
    /// <param name="language">The target language. Uses the current language of I18Next if not provided.</param>
    /// <returns>The translated error message or null if the attribute is not translated or no translation exists.</returns>
    public string GetErrorMessage(ValidationAttribute attribute, string displayName, string language = null)
    {
        return GetErrorMessage(attribute, displayName, null, language);
    }

    internal string GetErrorMessage(ValidationAttribute attribute, string displayName, string otherDisplayName, string language)
    {
        if (!CanTranslate(attribute))
            return null;

        language ??= GetLanguage();

        var mapping = Options.FindMapping(attribute.GetType());
        var arguments = new Dictionary<string, object> { ["field"] = displayName };

        mapping?.AddArguments(attribute, arguments);

        if (attribute is CompareAttribute compareAttribute)
            arguments["other"] = otherDisplayName ?? GetDisplayName(compareAttribute.OtherPropertyDisplayName ?? compareAttribute.OtherProperty, language);

        if (!HasCustomErrorMessage(attribute))
            return Translate(language, mapping.GetKey(attribute), arguments);

        return Exists(language, QualifyKey(Options.Namespace, attribute.ErrorMessage))
            ? _i18Next.T(language, Options.Namespace, attribute.ErrorMessage, arguments)
            : null;
    }

    internal string GetModelBindingMessage(string key, IDictionary<string, object> arguments)
    {
        return Translate(GetLanguage(), ModelBindingKeyPrefix + key, arguments);
    }

    private static string CreateDefaultErrorMessage(Type type)
    {
        var constructor = type.GetConstructor(Type.EmptyTypes);

        if (constructor != null)
            return ((ValidationAttribute)constructor.Invoke(null)).ErrorMessage;

        constructor = type.GetConstructor([typeof(object[])]);

        return constructor == null ? null : ((ValidationAttribute)constructor.Invoke([Array.Empty<object>()])).ErrorMessage;
    }

    private static bool HasCustomErrorMessage(ValidationAttribute attribute)
    {
        if (string.IsNullOrEmpty(attribute.ErrorMessage))
            return false;

        var type = attribute.GetType();

        return type.Assembly != typeof(ValidationAttribute).Assembly || attribute.ErrorMessage != DefaultErrorMessages.GetOrAdd(type, CreateDefaultErrorMessage);
    }

    private static string QualifyKey(string @namespace, string key)
    {
        return @namespace == null || key.IndexOf(':') > -1 ? key : @namespace + ":" + key;
    }

    private bool Exists(string language, string key)
    {
        try
        {
            return _i18Next.ExistsAsync(language, key).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (TranslationKeyInvalidException)
        {
            return false;
        }
    }

    private string GetLanguage()
    {
        if (_i18Next.DetectLanguageOnEachTranslation && _i18Next.LanguageDetector != null)
        {
            var detectedLanguage = _i18Next.LanguageDetector.GetLanguage();

            if (!string.IsNullOrWhiteSpace(detectedLanguage))
                return detectedLanguage;
        }

        return _i18Next.Language;
    }

    private string Translate(string language, string key, IDictionary<string, object> arguments)
    {
        var defaultMessage = Options.UseDefaultMessages ? DefaultValidationMessages.Get(language, key) : null;

        if (defaultMessage != null)
        {
            arguments["defaultValue"] = defaultMessage;

            return _i18Next.T(language, Options.Namespace, key, arguments);
        }

        return Exists(language, QualifyKey(Options.Namespace, key)) ? _i18Next.T(language, Options.Namespace, key, arguments) : null;
    }
}
