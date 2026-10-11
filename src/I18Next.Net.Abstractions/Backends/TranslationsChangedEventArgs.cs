using System;

namespace I18Next.Net.Backends;

/// <summary>
///     The data of the <see cref="INotifyingTranslationBackend.TranslationsChanged" /> event.
/// </summary>
/// <param name="language">
///     The changed language or <c>null</c> if all languages may have changed. A language without region also affects its
///     regional languages.
/// </param>
/// <param name="ns">The changed namespace or <c>null</c> if all namespaces may have changed.</param>
public class TranslationsChangedEventArgs(string language, string ns) : EventArgs
{
    /// <summary>
    ///     The changed language or <c>null</c> if all languages may have changed. A language without region (e.g. de) also
    ///     affects its regional languages (e.g. de-AT).
    /// </summary>
    public string Language { get; } = language;

    /// <summary>
    ///     The changed namespace or <c>null</c> if all namespaces may have changed.
    /// </summary>
    public string Namespace { get; } = ns;

    /// <summary>
    ///     Checks whether the change affects the translations of a namespace in a language, e.g. to drop a cached
    ///     namespace.
    /// </summary>
    /// <param name="language">The language of the namespace, e.g. <c>de-AT</c>. Compared case-insensitively.</param>
    /// <param name="ns">The namespace. Compared case-sensitively.</param>
    /// <returns>
    ///     Whether all namespaces or <paramref name="ns" /> changed, and all languages, <paramref name="language" /> or
    ///     its language part (e.g. <c>de</c> for <c>de-AT</c>) changed.
    /// </returns>
    public bool Affects(string language, string ns)
    {
        if (Namespace != null && !string.Equals(Namespace, ns, StringComparison.Ordinal))
            return false;

        if (Language == null || string.Equals(Language, language, StringComparison.OrdinalIgnoreCase))
            return true;

        var index = language.IndexOf('-');

        return index > 0 && string.Compare(language, 0, Language, 0, index, StringComparison.OrdinalIgnoreCase) == 0 && Language.Length == index;
    }
}
