using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I18Next.Net.Plugins;

public class PseudoLocalizationPostProcessor(PseudoLocalizationOptions options) : IPostProcessor
{
    public string Keyword => "pseudo";

    public PseudoLocalizationOptions Options { get; } = options;

    public string ProcessTranslation(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        return value;
    }

    public string ProcessResult(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        if (!Options.LanguagesToPseudo.Contains(language))
            return value;

        var output = new StringBuilder();

        foreach (var c in value)
        {
            var newChar = Options.Letters.TryGetValue(c, out var c2) ? c2 : c;

            if (Options.RepeatedLetters.Contains(c))
                output.Append([.. Enumerable.Repeat(newChar, Options.LetterMultiplier)]);
            else
                output.Append(newChar);
        }

        return output.ToString();
    }
}
