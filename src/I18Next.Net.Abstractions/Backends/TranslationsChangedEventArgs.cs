using System;

namespace I18Next.Net.Backends;

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
