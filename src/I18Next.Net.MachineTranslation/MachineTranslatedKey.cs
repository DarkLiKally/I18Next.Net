namespace I18Next.Net.MachineTranslation;

/// <summary>
///     A missing key translated by the <see cref="MachineTranslationMissingKeyHandler" />.
/// </summary>
/// <param name="language">The language the key was missing in.</param>
/// <param name="namespace">The namespace of the key.</param>
/// <param name="key">The key including plural or context suffixes.</param>
/// <param name="sourceLanguage">The language the text was translated from.</param>
/// <param name="sourceText">The text in the source language.</param>
/// <param name="text">The translated text.</param>
public class MachineTranslatedKey(string language, string @namespace, string key, string sourceLanguage, string sourceText, string text)
{
    /// <summary>
    ///     The language the key was missing in.
    /// </summary>
    public string Language { get; } = language;

    /// <summary>
    ///     The namespace of the key.
    /// </summary>
    public string Namespace { get; } = @namespace;

    /// <summary>
    ///     The key including plural or context suffixes.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>
    ///     The language the text was translated from.
    /// </summary>
    public string SourceLanguage { get; } = sourceLanguage;

    /// <summary>
    ///     The text in the source language.
    /// </summary>
    public string SourceText { get; } = sourceText;

    /// <summary>
    ///     The translated text.
    /// </summary>
    public string Text { get; } = text;
}
