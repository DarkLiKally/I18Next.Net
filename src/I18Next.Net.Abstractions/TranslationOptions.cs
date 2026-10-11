using System;
using System.Collections.Generic;

namespace I18Next.Net;

/// <summary>
///     The options of a translation request passed to the <see cref="Plugins.ITranslator" />: the namespace used for keys
///     without namespace prefix and the fallbacks used when a key is missing.
/// </summary>
public class TranslationOptions
{
    /// <summary>
    ///     The namespace used for keys without namespace prefix, like the i18next <c>defaultNS</c> option.
    /// </summary>
    /// <exception cref="ArgumentException">The value is set to <c>null</c>, empty or white space.</exception>
    public string DefaultNamespace
    {
        get;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(nameof(value));

            field = value;
        }
    }

    /// <summary>
    ///     The languages looked into in the given order when a key is missing in the requested language, like the
    ///     i18next <c>fallbackLng</c> option. Ignored for languages with an entry in <see cref="LanguageFallbacks" />.
    ///     <c>null</c> disables the fallback.
    /// </summary>
    public string[] FallbackLanguages { get; set; }

    /// <summary>
    ///     The namespaces looked into in the given order when a key is missing in the requested namespace, like the
    ///     i18next <c>fallbackNS</c> option. They are tried for each language before moving on to the next fallback
    ///     language. <c>null</c> disables the fallback.
    /// </summary>
    public string[] FallbackNamespaces { get; set; }

    /// <summary>
    ///     Fallback languages for specific languages, like the object form of the i18next <c>fallbackLng</c> option. An
    ///     entry for the requested language (e.g. <c>de-CH</c>) or else for its language part (e.g. <c>de</c>) is used
    ///     instead of <see cref="FallbackLanguages" />. <c>null</c> uses <see cref="FallbackLanguages" /> for all
    ///     languages.
    /// </summary>
    public IDictionary<string, string[]> LanguageFallbacks { get; set; }
}
