using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace I18Next.Net.MachineTranslation.Internal;

/// <summary>
///     Wraps i18next interpolations (<c>{{name}}</c>), nestings (<c>$t(key)</c>) and HTML tags in markup a translation
///     service leaves untouched and restores them in the translated text.
/// </summary>
internal sealed class PlaceholderProtector
{
    /// <summary>
    ///     Wraps protected parts in <c>&lt;x&gt;</c> elements for services with XML tag handling and ignored tags.
    /// </summary>
    public static readonly PlaceholderProtector Xml = new("x", "");

    /// <summary>
    ///     Wraps protected parts in <c>&lt;span class="notranslate"&gt;</c> elements for services translating HTML.
    /// </summary>
    public static readonly PlaceholderProtector Html = new("span", " class=\"notranslate\"");

    private readonly string _attributes;
    private readonly Regex _protectedPattern;

    private PlaceholderProtector(string element, string attributes)
    {
        Element = element;
        _attributes = attributes;
        _protectedPattern = new Regex($@"<{element}\b[^>]*?\sdata-i=""(\d+)""[^>]*?(?:/>|>.*?</{element}\s*>)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    public string Element { get; }

    public ProtectedText Protect(string text)
    {
        var builder = new StringBuilder(text.Length + 32);
        var tokens = new List<string>();
        var index = 0;

        while (index < text.Length)
        {
            var length = GetTokenLength(text, index);

            if (length == 0)
            {
                AppendEscaped(builder, text, index, 1);
                index++;
                continue;
            }

            builder.Append('<').Append(Element).Append(_attributes).Append(" data-i=\"").Append(tokens.Count.ToString(CultureInfo.InvariantCulture)).Append("\">");
            AppendEscaped(builder, text, index, length);
            builder.Append("</").Append(Element).Append('>');

            tokens.Add(text.Substring(index, length));
            index += length;
        }

        return new ProtectedText(builder.ToString(), tokens);
    }

    public string Restore(string translated, ProtectedText original)
    {
        var builder = new StringBuilder(translated.Length);
        var position = 0;

        foreach (Match match in _protectedPattern.Matches(translated))
        {
            builder.Append(WebUtility.HtmlDecode(translated.Substring(position, match.Index - position)));

            if (int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var tokenIndex) && tokenIndex < original.Tokens.Count)
                builder.Append(original.Tokens[tokenIndex]);

            position = match.Index + match.Length;
        }

        builder.Append(WebUtility.HtmlDecode(translated.Substring(position)));

        return builder.ToString();
    }

    internal static int GetTokenLength(string text, int index)
    {
        switch (text[index])
        {
            case '{' when index + 1 < text.Length && text[index + 1] == '{':
                var end = text.IndexOf("}}", index + 2, StringComparison.Ordinal);

                return end < 0 ? 0 : end + 2 - index;
            case '$' when string.CompareOrdinal(text, index, "$t(", 0, 3) == 0:
                return GetNestingLength(text, index);
            case '<':
                return GetTagLength(text, index);
            default:
                return 0;
        }
    }

    private static int GetNestingLength(string text, int index)
    {
        var depth = 0;
        var quote = '\0';

        for (var i = index + 2; i < text.Length; i++)
        {
            var c = text[i];

            if (quote != '\0')
            {
                if (c == '\\')
                    i++;
                else if (c == quote)
                    quote = '\0';

                continue;
            }

            switch (c)
            {
                case '"' or '\'':
                    quote = c;
                    break;
                case '(':
                    depth++;
                    break;
                case ')' when --depth == 0:
                    return i + 1 - index;
            }
        }

        return 0;
    }

    private static int GetTagLength(string text, int index)
    {
        var start = index + 1;

        if (start < text.Length && text[start] == '/')
            start++;

        if (start >= text.Length || !char.IsLetterOrDigit(text[start]))
            return 0;

        for (var i = start + 1; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '>':
                    return i + 1 - index;
                case '<':
                    return 0;
            }
        }

        return 0;
    }

    private static void AppendEscaped(StringBuilder builder, string text, int index, int length)
    {
        for (var i = index; i < index + length; i++)
        {
            switch (text[i])
            {
                case '&':
                    builder.Append("&amp;");
                    break;
                case '<':
                    builder.Append("&lt;");
                    break;
                case '>':
                    builder.Append("&gt;");
                    break;
                default:
                    builder.Append(text[i]);
                    break;
            }
        }
    }
}
