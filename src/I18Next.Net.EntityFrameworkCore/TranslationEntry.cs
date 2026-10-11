namespace I18Next.Net.EntityFrameworkCore;

/// <summary>
///     The translation of a key in a language and namespace stored in the database.
/// </summary>
public class TranslationEntry
{
    /// <summary>
    ///     The maximum length of the <see cref="Key" /> column.
    /// </summary>
    public const int KeyMaxLength = 500;

    /// <summary>
    ///     The maximum length of the <see cref="Language" /> column.
    /// </summary>
    public const int LanguageMaxLength = 35;

    /// <summary>
    ///     The maximum length of the <see cref="Namespace" /> column.
    /// </summary>
    public const int NamespaceMaxLength = 100;

    /// <summary>
    ///     The primary key. Translations of a namespace are loaded in the order of their ids.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The language, e.g. <c>de</c> or <c>de-AT</c>.
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    ///     The namespace, e.g. <c>translation</c> or <c>common</c>.
    /// </summary>
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
