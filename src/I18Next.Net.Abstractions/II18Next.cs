using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using I18Next.Net.Backends;
using I18Next.Net.Plugins;

namespace I18Next.Net;

/// <summary>
///     I18Next.Net instance.
/// </summary>
public interface II18Next
{
    /// <summary>
    ///     The backend used to resolve translations.
    /// </summary>
    ITranslationBackend Backend { get; }

    /// <summary>
    ///     Default namespace to be used to retrieve translations.
    /// </summary>
    string DefaultNamespace { get; set; }

    /// <summary>
    ///     Whether I18Next should re-detect the language for each translation request.
    ///     Note: Enabling this could have a huge performance impact depending on the number of translations.
    /// </summary>
    bool DetectLanguageOnEachTranslation { get; set; }

    /// <summary>
    ///     The language used to retrieve translations from the backend.
    /// </summary>
    string Language { get; set; }

    /// <summary>
    ///     The language detector used to detect the language to be used for retrieving translations.
    /// </summary>
    ILanguageDetector LanguageDetector { get; }

    /// <summary>
    ///     The translator used to resolve translations from the backend.
    /// </summary>
    ITranslator Translator { get; }

    /// <summary>
    ///     Event fired when the default language has changed.
    /// </summary>
    event EventHandler<LanguageChangedEventArgs> LanguageChanged;

    /// <summary>
    ///     Translates the given key into the default language.
    /// </summary>
    /// <param name="key">Key to be translated.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    string T(string key, object args = null);

    /// <summary>
    ///     Translates the given key into the provided language.
    /// </summary>
    /// <param name="language">Target language override.</param>
    /// <param name="key">Key to be translated.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    string T(string language, string key, object args = null);

    /// <summary>
    ///     Translates the given key into the provided language using another default namespace.
    /// </summary>
    /// <param name="language">Target language override.</param>
    /// <param name="defaultNamespace">Default namespace override.</param>
    /// <param name="key">Key to be translated.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    string T(string language, string defaultNamespace, string key, object args = null);

    /// <summary>
    ///     Translates the given key into the default language.
    /// </summary>
    /// <param name="key">Key to be translated.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    Task<string> Ta(string key, object args = null);

    /// <summary>
    ///     Translates the given key into the default language.
    /// </summary>
    /// <param name="language">Target language override.</param>
    /// <param name="key">Key to be translated.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    Task<string> Ta(string language, string key, object args = null);

    /// <summary>
    ///     Translates the given key into the provided language using another default namespace.
    /// </summary>
    /// <param name="language">Target language override.</param>
    /// <param name="defaultNamespace">Default namespace override.</param>
    /// <param name="key">Key to be translated.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    Task<string> Ta(string language, string defaultNamespace, string key, object args = null);

    /// <summary>
    ///     Translates the first of the given keys which exists in the default language.
    /// </summary>
    /// <param name="keys">Keys to be translated in order of preference.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    string T(string[] keys, object args = null);

    /// <summary>
    ///     Translates the first of the given keys which exists in the default language.
    /// </summary>
    /// <param name="keys">Keys to be translated in order of preference.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    Task<string> Ta(string[] keys, object args = null);

    /// <summary>
    ///     Translates all values below the given key into the default language. Nested groups are returned as dictionaries,
    ///     arrays as object arrays.
    /// </summary>
    /// <param name="key">Key of the group to be translated.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <returns>The translated values or null if the key does not lead to a group.</returns>
    IDictionary<string, object> TObject(string key, object args = null);

    /// <summary>
    ///     Translates all values below the given key into the provided language. Nested groups are returned as dictionaries,
    ///     arrays as object arrays.
    /// </summary>
    /// <param name="language">Target language override.</param>
    /// <param name="key">Key of the group to be translated.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <returns>The translated values or null if the key does not lead to a group.</returns>
    IDictionary<string, object> TObject(string language, string key, object args = null);

    /// <summary>
    ///     Translates all values below the given key into the default language.
    /// </summary>
    /// <param name="key">Key of the group to be translated.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <returns>The translated values or null if the key does not lead to a group.</returns>
    Task<IDictionary<string, object>> TaObject(string key, object args = null);

    /// <summary>
    ///     Translates all values below the given key into the provided language.
    /// </summary>
    /// <param name="language">Target language override.</param>
    /// <param name="key">Key of the group to be translated.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <returns>The translated values or null if the key does not lead to a group.</returns>
    Task<IDictionary<string, object>> TaObject(string language, string key, object args = null);

    /// <summary>
    ///     Translates all values below the given key into the default language and maps them to the given type.
    /// </summary>
    /// <param name="key">Key of the group to be translated.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <typeparam name="TModel">The type the translated values are mapped to.</typeparam>
    /// <returns>The mapped translations or the default value if the key does not lead to a group.</returns>
    TModel T<TModel>(string key, object args = null);

    /// <summary>
    ///     Translates all values below the given key into the provided language and maps them to the given type.
    /// </summary>
    /// <param name="language">Target language override.</param>
    /// <param name="key">Key of the group to be translated.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <typeparam name="TModel">The type the translated values are mapped to.</typeparam>
    /// <returns>The mapped translations or the default value if the key does not lead to a group.</returns>
    TModel T<TModel>(string language, string key, object args = null);

    /// <summary>
    ///     Translates all values below the given key into the default language and maps them to the given type.
    /// </summary>
    /// <param name="key">Key of the group to be translated.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <typeparam name="TModel">The type the translated values are mapped to.</typeparam>
    /// <returns>The mapped translations or the default value if the key does not lead to a group.</returns>
    Task<TModel> Ta<TModel>(string key, object args = null);

    /// <summary>
    ///     Translates all values below the given key into the provided language and maps them to the given type.
    /// </summary>
    /// <param name="language">Target language override.</param>
    /// <param name="key">Key of the group to be translated.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <typeparam name="TModel">The type the translated values are mapped to.</typeparam>
    /// <returns>The mapped translations or the default value if the key does not lead to a group.</returns>
    Task<TModel> Ta<TModel>(string language, string key, object args = null);

    /// <summary>
    ///     Checks whether a translation exists for the given key in the default language.
    /// </summary>
    /// <param name="key">Key to be checked.</param>
    /// <param name="args">Additional arguments used to resolve the key, e.g. count or context.</param>
    /// <returns>Whether a translation exists.</returns>
    bool Exists(string key, object args = null);

    /// <summary>
    ///     Checks whether a translation exists for the given key in the provided language.
    /// </summary>
    /// <param name="language">Target language override.</param>
    /// <param name="key">Key to be checked.</param>
    /// <param name="args">Additional arguments used to resolve the key, e.g. count or context.</param>
    /// <returns>Whether a translation exists.</returns>
    Task<bool> ExistsAsync(string language, string key, object args = null);

    /// <summary>
    ///     Gets the text direction of the given language.
    /// </summary>
    /// <param name="language">The language to check. Uses the default language if not provided.</param>
    /// <returns>"rtl" for right-to-left languages, otherwise "ltr".</returns>
    string Dir(string language = null);

    /// <summary>
    ///     Uses the registered language detector to detect the language and set it as the default language.
    /// </summary>
    void UseDetectedLanguage();
}