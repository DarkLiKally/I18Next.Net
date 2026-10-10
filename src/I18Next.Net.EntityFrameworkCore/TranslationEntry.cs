namespace I18Next.Net.EntityFrameworkCore;

/// <summary>
///     The translation of a key in a language and namespace stored in the database.
/// </summary>
public class TranslationEntry
{
    public const int KeyMaxLength = 500;

    public const int LanguageMaxLength = 35;

    public const int NamespaceMaxLength = 100;

    public int Id { get; set; }

    /// <summary>
    ///     The language, e.g. <c>de</c> or <c>de-AT</c>.
    /// </summary>
    public string Language { get; set; }

    public string Namespace { get; set; }

    /// <summary>
    ///     The key with dots separating nested groups, e.g. <c>menu.home</c>.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    ///     The translation. <c>null</c> marks a missing key which still has to be translated, it is not loaded.
    /// </summary>
    public string Value { get; set; }
}
