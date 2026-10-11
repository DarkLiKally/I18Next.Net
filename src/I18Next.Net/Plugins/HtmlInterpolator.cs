using System.Collections.Generic;

using I18Next.Net.Logging;

namespace I18Next.Net.Plugins;

/// <summary>
///     An interpolator which HTML-escapes inserted values like i18next does by default, escaping <c>&amp;</c>,
///     <c>&lt;</c>, <c>&gt;</c>, <c>"</c>, <c>'</c> and <c>/</c>. Values of variables with the
///     <see cref="DefaultInterpolator.UnescapePrefix" />, e.g. <c>{{- html}}</c>, are inserted unescaped.
/// </summary>
/// <param name="logger">The logger used by the default formatter.</param>
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

    /// <inheritdoc />
    protected override string EscapeValue(string value)
    {
        foreach (var token in TokenMap)
            value = value.Replace(token.Key, token.Value);

        return value;
    }
}
