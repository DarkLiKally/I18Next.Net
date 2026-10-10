using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace I18Next.Net.Tool.Extraction;

/// <summary>
///     Finds translation keys in Razor views and components with regular expressions. Only keys passed as the first
///     string literal of a translation method or a localizer indexer are found.
/// </summary>
internal sealed class MarkupKeyExtractor
{
    private const string StringLiteral = "\"(?<key>(?:[^\"\\\\\\r\\n]|\\\\.)*)\"";

    private static readonly Regex CountPattern = new(@"\bcount\b", RegexOptions.CultureInvariant);
    private static readonly Regex OrdinalPattern = new(@"\bordinal\s*=\s*true\b", RegexOptions.CultureInvariant);
    private static readonly Regex ContextPattern = new("\\bcontext\\s*=\\s*\"(?<context>[^\"]*)\"", RegexOptions.CultureInvariant);
    private static readonly Regex EscapePattern = new(@"\\(.)", RegexOptions.CultureInvariant);

    private readonly ExtractionOptions _options;
    private readonly Regex _functionPattern;
    private readonly Regex _localizerPattern;

    public MarkupKeyExtractor(ExtractionOptions options)
    {
        _options = options;

        var functions = string.Join("|", options.FunctionNames.OrderByDescending(n => n.Length).Select(Regex.Escape));
        _functionPattern = new Regex($@"(?<!\w|\bFile\.|\bDirectory\.)(?<name>{functions})(?<generic><[^<>()""]*>)?\(\s*{StringLiteral}",
            RegexOptions.CultureInvariant);

        var localizers = string.Join("|", options.LocalizerNames.Select(Regex.Escape).Append(@"\w*[Ll]ocalizer"));
        _localizerPattern = new Regex($@"(?<!\w)(?:{localizers})\[\s*{StringLiteral}", RegexOptions.CultureInvariant);
    }

    public IEnumerable<ExtractedKey> Extract(string text, string file)
    {
        foreach (var pattern in new[] { _functionPattern, _localizerPattern })
        {
            foreach (Match match in pattern.Matches(text))
            {
                var name = match.Groups["name"].Value;
                var key = EscapePattern.Replace(match.Groups["key"].Value, "$1");
                var args = GetArguments(text, match.Index + match.Length);
                var @namespace = _options.DefaultNamespace;
                var separatorIndex = string.IsNullOrEmpty(_options.NamespaceSeparator) ? -1 : key.IndexOf(_options.NamespaceSeparator, StringComparison.Ordinal);

                if (separatorIndex > 0)
                {
                    @namespace = key.Substring(0, separatorIndex);
                    key = key.Substring(separatorIndex + _options.NamespaceSeparator.Length);
                }

                var context = ContextPattern.Match(args);

                yield return new ExtractedKey(@namespace, key, file, GetLine(text, match.Index))
                {
                    HasCount = CountPattern.IsMatch(args),
                    Ordinal = OrdinalPattern.IsMatch(args),
                    Context = context.Success ? context.Groups["context"].Value : null,
                    ReturnsObject = name is "TObject" or "TaObject" || match.Groups["generic"].Success
                };
            }
        }
    }

    /// <summary>
    ///     Gets the text of the remaining arguments up to the closing parenthesis or bracket of the call.
    /// </summary>
    private static string GetArguments(string text, int start)
    {
        var depth = 0;

        for (var i = start; i < text.Length && i < start + 1000; i++)
        {
            switch (text[i])
            {
                case '(' or '[' or '{':
                    depth++;
                    break;
                case ')' or ']' or '}' when depth == 0:
                    return text.Substring(start, i - start);
                case ')' or ']' or '}':
                    depth--;
                    break;
            }
        }

        return string.Empty;
    }

    private static int GetLine(string text, int index)
    {
        var line = 1;

        for (var i = 0; i < index; i++)
        {
            if (text[i] == '\n')
                line++;
        }

        return line;
    }
}
