using System;
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
    ///     Uses the registered language detector to detect the language and set it as the default language.
    /// </summary>
    void UseDetectedLanguage();
}