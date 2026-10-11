namespace I18Next.Net.Plugins;

/// <summary>
///     A language detector plugin which determines the language to translate into, e.g. from the current culture or the
///     current request.
/// </summary>
public interface ILanguageDetector
{
    /// <summary>
    ///     Detects the language to translate into. <see cref="II18Next.UseDetectedLanguage" /> assigns the result to
    ///     <see cref="II18Next.Language" />, and with <see cref="II18Next.DetectLanguageOnEachTranslation" /> enabled
    ///     it is called for each translation.
    /// </summary>
    /// <returns>
    ///     The detected language code, e.g. <c>en-US</c> or <c>de</c>. When translating with
    ///     <see cref="II18Next.DetectLanguageOnEachTranslation" /> enabled, <c>null</c> or an empty value falls back to
    ///     <see cref="II18Next.Language" />, but <see cref="II18Next.UseDetectedLanguage" /> rejects it.
    /// </returns>
    string GetLanguage();
}
