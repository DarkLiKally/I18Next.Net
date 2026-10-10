using System;
using System.Threading;
using System.Threading.Tasks;

namespace I18Next.Net.MachineTranslation;

public static class MachineTranslatorExtensions
{
    /// <summary>
    ///     Translates a single text.
    /// </summary>
    /// <param name="translator">The translator to use.</param>
    /// <param name="text">The text to translate.</param>
    /// <param name="sourceLanguage">The language of the text or <c>null</c> to detect it.</param>
    /// <param name="targetLanguage">The language to translate the text into.</param>
    /// <param name="cancellationToken">Cancels the translation.</param>
    /// <returns>The translated text.</returns>
    public static async Task<string> TranslateAsync(this IMachineTranslator translator, string text, string sourceLanguage, string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        if (translator == null)
            throw new ArgumentNullException(nameof(translator));
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        var result = await translator.TranslateAsync([text], sourceLanguage, targetLanguage, cancellationToken).ConfigureAwait(false);

        return result[0];
    }
}
