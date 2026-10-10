using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace I18Next.Net.MachineTranslation;

/// <summary>
///     Translates texts through a delegate, e.g. to use a language model through its own SDK. The texts are passed as they
///     are, so the delegate has to keep i18next placeholders like <c>{{name}}</c> and <c>$t(key)</c> intact.
/// </summary>
public class FuncMachineTranslator : IMachineTranslator
{
    private readonly Func<IReadOnlyList<string>, string, string, CancellationToken, Task<IReadOnlyList<string>>> _translate;

    /// <param name="translate">Translates the texts (first argument) from the source language (second) into the target language (third).</param>
    public FuncMachineTranslator(Func<IReadOnlyList<string>, string, string, CancellationToken, Task<IReadOnlyList<string>>> translate)
    {
        _translate = translate ?? throw new ArgumentNullException(nameof(translate));
    }

    public async Task<IReadOnlyList<string>> TranslateAsync(IReadOnlyList<string> texts, string sourceLanguage, string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        if (texts == null)
            throw new ArgumentNullException(nameof(texts));

        if (texts.Count == 0)
            return [];

        var result = await _translate(texts, sourceLanguage, targetLanguage, cancellationToken).ConfigureAwait(false);

        if (result == null || result.Count != texts.Count)
            throw new MachineTranslationException($"The translation delegate returned {result?.Count ?? 0} texts for {texts.Count} texts.");

        return result;
    }
}
