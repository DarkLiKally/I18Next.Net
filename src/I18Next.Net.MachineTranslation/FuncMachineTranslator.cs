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

    /// <summary>
    ///     Creates a translator calling the given delegate.
    /// </summary>
    /// <param name="translate">
    ///     Translates the texts (first argument) from the source language (second, <c>null</c> to detect it) into the target
    ///     language (third) and returns the translated texts in the same order.
    /// </param>
    /// <exception cref="ArgumentNullException">If <paramref name="translate" /> is <c>null</c>.</exception>
    public FuncMachineTranslator(Func<IReadOnlyList<string>, string, string, CancellationToken, Task<IReadOnlyList<string>>> translate)
    {
        _translate = translate ?? throw new ArgumentNullException(nameof(translate));
    }

    /// <inheritdoc />
    /// <remarks>
    ///     The delegate is not called for an empty list of texts.
    /// </remarks>
    /// <exception cref="ArgumentNullException">If <paramref name="texts" /> is <c>null</c>.</exception>
    /// <exception cref="MachineTranslationException">
    ///     If the delegate returns <c>null</c> or a different number of texts than it received.
    /// </exception>
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
