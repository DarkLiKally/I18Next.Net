using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using I18Next.Net.Formatters;
using I18Next.Net.Internal;
using I18Next.Net.Logging;

namespace I18Next.Net.Plugins;

public class DefaultInterpolator : IInterpolator
{
    private readonly ILogger _logger;
    private const string DefaultPrefix = "{{";
    private const string DefaultSuffix = "}}";
    private const string DefaultUnescapePrefix = "-";
    private const string DefaultNestingPrefix = "$t(";
    private const string DefaultNestingSuffix = ")";

    private static readonly Regex DefaultExpressionRegex = CreateExpressionRegex(DefaultPrefix, DefaultSuffix);

    private static readonly Regex DefaultUnescapedExpressionRegex = CreateExpressionRegex(DefaultPrefix + DefaultUnescapePrefix, DefaultSuffix);

    private static readonly Regex DefaultNestingRegex = CreateExpressionRegex(DefaultNestingPrefix, DefaultNestingSuffix);

    private static readonly JsonDocumentOptions NestedArgsDocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };


    private List<IFormatter> _formatters;
    private Regex _expressionRegex = DefaultExpressionRegex;
    private Regex _unescapedExpressionRegex = DefaultUnescapedExpressionRegex;
    private Regex _nestingRegex = DefaultNestingRegex;
    private string _prefix = DefaultPrefix;
    private string _suffix = DefaultSuffix;
    private string _unescapePrefix = DefaultUnescapePrefix;
    private string _nestingPrefix = DefaultNestingPrefix;
    private string _nestingSuffix = DefaultNestingSuffix;

    public string Prefix
    {
        get => _prefix;
        set
        {
            _prefix = ValidateDelimiter(value);
            UpdateExpressionRegexes();
        }
    }

    public string Suffix
    {
        get => _suffix;
        set
        {
            _suffix = ValidateDelimiter(value);
            UpdateExpressionRegexes();
        }
    }

    public string UnescapePrefix
    {
        get => _unescapePrefix;
        set
        {
            _unescapePrefix = ValidateDelimiter(value);
            UpdateExpressionRegexes();
        }
    }

    public string NestingPrefix
    {
        get => _nestingPrefix;
        set
        {
            _nestingPrefix = ValidateDelimiter(value);
            _nestingRegex = CreateExpressionRegex(_nestingPrefix, _nestingSuffix);
        }
    }

    public string NestingSuffix
    {
        get => _nestingSuffix;
        set
        {
            _nestingSuffix = ValidateDelimiter(value);
            _nestingRegex = CreateExpressionRegex(_nestingPrefix, _nestingSuffix);
        }
    }

    public IFormatter DefaultFormatter { get; set; }

    public bool EscapeValues { get; set; } = true;

    public string FormatSeparator { get; set; } = ",";

    public int MaximumReplaces { get; set; } = 1000;

    public Func<string, Match, string> MissingValueHandler { get; set; }

    public bool UseFastNestingMatch { get; set; } = true;

    public DefaultInterpolator(ILogger logger)
    {
        _logger = logger;
        DefaultFormatter = new DefaultFormatter(_logger);
    }

    public virtual bool CanNest(string source)
    {
        return UseFastNestingMatch ? source.Contains(_nestingPrefix) : _nestingRegex.IsMatch(source);
    }

    public List<IFormatter> Formatters => _formatters ?? (_formatters = new List<IFormatter>());

    public virtual Task<string> InterpolateAsync(string source, string key, string language, IDictionary<string, object> args)
    {
        if (!source.Contains(_prefix))
            return Task.FromResult(source);

        var matches = _expressionRegex.Matches(source);

        var result = source;
        var replaces = 0;

        if (source.Contains(_prefix + _unescapePrefix))
        {
            var unescapeMatches = _unescapedExpressionRegex.Matches(source);

            for (var i = 0; i < unescapeMatches.Count; i++)
            {
                var match = unescapeMatches[i];
                result = HandleUnescapeRegexMatch(result, language, args, match);

                replaces++;

                if (replaces >= MaximumReplaces)
                    break;
            }
        }

        for (var i = 0; i < matches.Count; i++)
        {
            if (replaces >= MaximumReplaces)
                break;

            var match = matches[i];
            result = HandleRegexMatch(result, language, args, match);

            replaces++;
        }

        return Task.FromResult(result);
    }

    public virtual async Task<string> NestAsync(
        string source,
        string language,
        IDictionary<string, object> args,
        TranslateAsyncDelegate translateAsync)
    {
        var matches = _nestingRegex.Matches(source);

        var result = source;

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            result = await HandleNestingRegexMatchAsync(result, language, args, translateAsync, match).ConfigureAwait(false);
        }

        return result;
    }

    private static Regex CreateExpressionRegex(string prefix, string suffix)
    {
        return new Regex($"{Regex.Escape(prefix)}(.+?){Regex.Escape(suffix)}", RegexOptions.Compiled);
    }

    private static string ValidateDelimiter(string value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(nameof(value));

        return value;
    }

    private void UpdateExpressionRegexes()
    {
        _expressionRegex = CreateExpressionRegex(_prefix, _suffix);
        _unescapedExpressionRegex = CreateExpressionRegex(_prefix + _unescapePrefix, _suffix);
    }

    protected virtual string EscapeValue(string value)
    {
        return value;
    }

    protected virtual string Format(object value, string format, string language)
    {
        if (_formatters != null)
            for (var i = 0; i < _formatters.Count; i++)
            {
                var formatter = _formatters[i];

                if (formatter.CanFormat(value, format, language))
                    return formatter.Format(value, format, language);
            }

        return DefaultFormatter.Format(value, format, language);
    }

    protected static object GetValue(string key, IDictionary<string, object> args)
    {
        if (args == null)
            return null;

        if (key.IndexOf('.') < 0)
            return args.TryGetValue(key, out var value) ? value : null;

        var keyParts = key.Split('.');

        object lastObject = args;

        for (var i = 0; i < keyParts.Length; i++)
        {
            if (lastObject == null)
                return null;

            var subKey = keyParts[i];

            if (lastObject is IDictionary<string, object> dict)
            {
                if (!dict.TryGetValue(subKey, out lastObject))
                    return null;

                continue;
            }

            var lastObjectType = lastObject.GetType();

            if (lastObjectType.IsClass && lastObjectType.Namespace == null)
            {
                var lastDict = lastObject.ToDictionary();

                if (!lastDict.TryGetValue(subKey, out lastObject))
                    return null;
            }
        }

        return lastObject;
    }

    protected virtual string GetValueForExpression(string key, string language, IDictionary<string, object> args)
    {
        key = key.Trim();

        if (key.IndexOf(FormatSeparator, StringComparison.Ordinal) < 0)
            return GetValue(key, args)?.ToString();

        var keyParts = key.Split(FormatSeparator, 2);
        var actualKey = keyParts[0].Trim();
        var format = keyParts[1].Trim();
        var value = GetValue(actualKey, args);

        return Format(value, format, language);
    }

    protected virtual async Task<string> HandleNestingRegexMatchAsync(
        string source,
        string language,
        IDictionary<string, object> args,
        TranslateAsyncDelegate translateAsync,
        Match match)
    {
        var expression = match.Groups[1];
        string key;
        IDictionary<string, object> childArgs;

        if (expression.Value.IndexOf(FormatSeparator, StringComparison.Ordinal) < 0)
        {
            key = expression.Value;
            childArgs = args;
        }
        else
        {
            var keyParts = expression.Value.Split(FormatSeparator, 2);
            key = keyParts[0];

            var childArgsString = keyParts[1].Trim();
            childArgs = await ParseNestedArgsAsync(childArgsString, language, args).ConfigureAwait(false);
        }

        var value = await translateAsync(language, key, childArgs).ConfigureAwait(false);

        if (value == null)
            return source;

        if (value.Contains(match.Value))
            return source;

        return source.ReplaceFirst(match.Value, value);
    }

    protected virtual string HandleRegexMatch(string source, string language, IDictionary<string, object> args, Match match)
    {
        var expression = match.Groups[1];
        var value = GetValueForExpression(expression.Value, language, args);

        if (value == null)
            if (MissingValueHandler != null)
                value = MissingValueHandler(source, match);
            else
                value = string.Empty;

        if (EscapeValues)
            value = EscapeValue(value);

        source = source.ReplaceFirst(match.Value, value);
        return source;
    }

    protected virtual string HandleUnescapeRegexMatch(string source, string language, IDictionary<string, object> args, Match match)
    {
        var expression = match.Groups[1];
        var value = GetValueForExpression(expression.Value, language, args);

        if (value == null)
            if (MissingValueHandler != null)
                value = MissingValueHandler(source, match);
            else
                value = string.Empty;

        source = source.ReplaceFirst(match.Value, value);
        return source;
    }

    protected virtual async Task<IDictionary<string, object>> ParseNestedArgsAsync(
        string argsString,
        string language,
        IDictionary<string, object> parentArgs)
    {
        argsString = await InterpolateAsync(argsString, null, language, parentArgs).ConfigureAwait(false);
        argsString = argsString.Replace('\'', '"');

        IDictionary<string, object> args;

        using (var document = JsonDocument.Parse(argsString, NestedArgsDocumentOptions))
        {
            args = document.RootElement.ToDictionary();
        }

        if (parentArgs != null)
            args = parentArgs.MergeLeft(args);

        return args;
    }
}
