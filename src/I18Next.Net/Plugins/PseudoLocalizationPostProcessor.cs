using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I18Next.Net.Plugins;

/// <summary>
///     Pseudo-localizes the translations of the configured languages by replacing letters with accented look-alikes and
///     repeating vowels, to find hard-coded texts and layouts not prepared for longer translations. Enabled per
///     translation with the <c>postProcess: "pseudo"</c> argument.
/// </summary>
/// <param name="options">The options selecting the languages and the replacements.</param>
public class PseudoLocalizationPostProcessor(PseudoLocalizationOptions options) : IPostProcessor
{
    /// <inheritdoc />
    public string Keyword => "pseudo";

    /// <summary>
    ///     The options selecting the languages and the replacements.
    /// </summary>
    public PseudoLocalizationOptions Options { get; } = options;

    /// <inheritdoc />
    public string ProcessTranslation(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        return value;
    }

    /// <inheritdoc />
    public string ProcessResult(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        if (!Options.LanguagesToPseudo.Contains(language))
            return value;

        var output = new StringBuilder();

        if (Options.WrapStrings)
            output.Append('[');

        foreach (var c in value)
        {
            var newChar = Options.Letters.TryGetValue(c, out var c2) ? c2 : c;

            if (Options.RepeatedLetters.Contains(c))
                output.Append([.. Enumerable.Repeat(newChar, Options.LetterMultiplier)]);
            else
                output.Append(newChar);
        }

        if (Options.WrapStrings)
            output.Append(']');

        return output.ToString();
    }
}
