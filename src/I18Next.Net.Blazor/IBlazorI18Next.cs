using System;
using System.Threading.Tasks;

using I18Next.Net.Backends;

namespace I18Next.Net.Blazor;

/// <summary>
///     The language of the current user (a circuit on the server or the WebAssembly application) on top of the shared
///     <see cref="II18Next" /> instance.
/// </summary>
public interface IBlazorI18Next
{
    /// <summary>
    ///     The shared I18Next instance. Its <see cref="II18Next.Language" /> is not the language of the current user.
    /// </summary>
    II18Next Instance { get; }

    /// <summary>
    ///     The language of the current user.
    /// </summary>
    string Language { get; }

    /// <summary>
    ///     Event fired when the language of the current user has changed.
    /// </summary>
    event EventHandler<LanguageChangedEventArgs> LanguageChanged;

    /// <summary>
    ///     Event fired when the translations of the backend have changed, e.g. because a watched translation file was edited.
    /// </summary>
    event EventHandler<TranslationsChangedEventArgs> TranslationsChanged;

    /// <summary>
    ///     Uses the language stored in the browser and loads the configured namespaces. Call it before the WebAssembly
    ///     application runs, as translations cannot be loaded over HTTP while rendering.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    ///     Loads the configured namespaces of the new language, changes the language of the current user and stores it in
    ///     the browser.
    /// </summary>
    /// <param name="language">The new language. It is matched against the supported languages.</param>
    Task ChangeLanguageAsync(string language);

    /// <summary>
    ///     Loads the given namespaces of the current language and its fallback languages, so they can be translated
    ///     synchronously.
    /// </summary>
    /// <param name="namespaces">The namespaces to load. Uses the configured namespaces if none are provided.</param>
    Task LoadNamespacesAsync(params string[] namespaces);

    /// <summary>
    ///     Translates the given key into the language of the current user.
    /// </summary>
    /// <param name="key">Key to be translated, optionally prefixed with a namespace like <c>common:save</c>.</param>
    /// <param name="args">Additional arguments used to translate the key.</param>
    /// <returns>Translation value.</returns>
    string T(string key, object args = null);

    /// <summary>
    ///     Translates all values below the given key into the language of the current user and maps them to the given type.
    /// </summary>
    /// <param name="key">Key of the group to be translated.</param>
    /// <param name="args">Additional arguments used to translate the values.</param>
    /// <typeparam name="TModel">The type the translated values are mapped to.</typeparam>
    /// <returns>The mapped translations or the default value if the key does not lead to a group.</returns>
    TModel T<TModel>(string key, object args = null);

    /// <summary>
    ///     Checks whether a translation exists for the given key in the language of the current user.
    /// </summary>
    /// <param name="key">Key to be checked.</param>
    /// <param name="args">Additional arguments used to resolve the key, e.g. count or context.</param>
    /// <returns>Whether a translation exists.</returns>
    bool Exists(string key, object args = null);

    /// <summary>
    ///     Returns a translator with the current language of the user and a fixed namespace and key prefix. It keeps the
    ///     language when the language of the user changes.
    /// </summary>
    FixedT GetFixedT(string @namespace = null, string keyPrefix = null);

    /// <summary>
    ///     Gets the text direction of the language of the current user.
    /// </summary>
    /// <returns>"rtl" for right-to-left languages, otherwise "ltr".</returns>
    string Dir();
}
