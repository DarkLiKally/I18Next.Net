using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace I18Next.Net.MachineTranslation;

/// <summary>
///     Translates texts from one language into another, e.g. with a machine translation service or a language model.
/// </summary>
public interface IMachineTranslator
{
    /// <summary>
    ///     Translates a batch of texts.
    /// </summary>
    /// <param name="texts">The texts to translate.</param>
    /// <param name="sourceLanguage">The language of the texts or <c>null</c> to detect it.</param>
    /// <param name="targetLanguage">The language to translate the texts into.</param>
    /// <param name="cancellationToken">Cancels the translation.</param>
    /// <returns>The translated texts in the order of <paramref name="texts" />.</returns>
    Task<IReadOnlyList<string>> TranslateAsync(IReadOnlyList<string> texts, string sourceLanguage, string targetLanguage,
        CancellationToken cancellationToken = default);
}
