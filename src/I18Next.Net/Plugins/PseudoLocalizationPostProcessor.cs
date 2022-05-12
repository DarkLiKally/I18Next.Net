using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace I18Next.Net.Plugins;

public class PseudoLocalizationPostProcessor : IPostProcessor
{
    private readonly PseudoLocalizationOptions _options;

    public string Keyword => "pseudo";

    public PseudoLocalizationOptions Options => _options;

    public PseudoLocalizationPostProcessor(PseudoLocalizationOptions options)
    {
        _options = options;
    }

    public string ProcessTranslation(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        return value;
    }

    public string ProcessResult(string key, string value, IDictionary<string, object> args, string language, ITranslator translator)
    {
        if (!_options.LanguagesToPseudo.Contains(language))
            return value;

        var output = new StringBuilder();
        
        foreach (var c in value)
        {
            var newChar = _options.Letters.TryGetValue(c, out var c2) ? c2 : c;

            if (_options.RepeatedLetters.Contains(c))
                output.Append(Enumerable.Repeat(newChar, _options.LetterMultiplier).ToArray());
            else
                output.Append(newChar);
        }

        return output.ToString();
    }
}
