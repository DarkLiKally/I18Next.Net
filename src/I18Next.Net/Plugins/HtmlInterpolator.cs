using System.Collections.Generic;

using I18Next.Net.Logging;

namespace I18Next.Net.Plugins;

public class HtmlInterpolator(ILogger logger) : DefaultInterpolator(logger)
{
    private static readonly Dictionary<string, string> TokenMap = new()
    {
        { "&", "&amp;" },
        { "<", "&lt;" },
        { ">", "&gt;" },
        { "\"", "&quot;" },
        { "'", "&#39;" },
        { "/", "&#x2F;" }
    };

    protected override string EscapeValue(string value)
    {
        foreach (var token in TokenMap)
            value = value.Replace(token.Key, token.Value);

        return value;
    }
}
