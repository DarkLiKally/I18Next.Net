namespace I18Next.Net.MachineTranslation;

/// <summary>
///     A missing key translated by the <see cref="MachineTranslationMissingKeyHandler" />.
/// </summary>
public class MachineTranslatedKey(string language, string @namespace, string key, string sourceLanguage, string sourceText, string text)
{
    /// <summary>
    ///     The language the key was missing in.
    /// </summary>
    public string Language { get; } = language;

    public string Namespace { get; } = @namespace;

    /// <summary>
    ///     The key including plural or context suffixes.
    /// </summary>
    public string Key { get; } = key;

    public string SourceLanguage { get; } = sourceLanguage;

    public string SourceText { get; } = sourceText;

    /// <summary>
    ///     The translated text.
    /// </summary>
    public string Text { get; } = text;
}
