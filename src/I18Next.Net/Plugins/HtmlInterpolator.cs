using System.Collections.Generic;

namespace I18Next.Net.Plugins;

public class HtmlInterpolator : DefaultInterpolator
{
    private static readonly Dictionary<string, string> TokenMap = new Dictionary<string, string>
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