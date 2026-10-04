using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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

    private static readonly Regex DefaultNestingRegex = CreateExpressionRegex(DefaultNestingPrefix, DefaultNestingSuffix);

    private static readonly JsonDocumentOptions NestedArgsDocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };


    private readonly System.Collections.Concurrent.ConcurrentDictionary<(string Expression, string Separator), (string Key, string Format)> _expressions = new();
    [ThreadStatic]
    private static StringBuilder _cachedBuilder;

    private List<IFormatter> _formatters;
    private Regex _expressionRegex = DefaultExpressionRegex;
    private Regex _nestingRegex = DefaultNestingRegex;

    public string Prefix
    {
        get;
        set
        {
            field = ValidateDelimiter(value);
            UpdateExpressionRegexes();
        }
    } = DefaultPrefix;

    public string Suffix
    {
        get;
        set
        {
            field = ValidateDelimiter(value);
            UpdateExpressionRegexes();
        }
    } = DefaultSuffix;

    public string UnescapePrefix
    {
        get;
        set
        {
            field = ValidateDelimiter(value);
            UpdateExpressionRegexes();
        }
    } = DefaultUnescapePrefix;

    public string NestingPrefix
    {
        get;
        set
        {
            field = ValidateDelimiter(value);
            _nestingRegex = CreateExpressionRegex(field, NestingSuffix);
        }
    } = DefaultNestingPrefix;

    public string NestingSuffix
    {
        get;
        set
        {
            field = ValidateDelimiter(value);
            _nestingRegex = CreateExpressionRegex(NestingPrefix, field);
        }
    } = DefaultNestingSuffix;

    public IFormatter DefaultFormatter { get; set; }

    public ISet<string> ChainableFormats { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "number", "currency", "datetime", "relativetime", "list", "lowercase", "uppercase"
    };

    public bool EscapeValues { get; set; } = true;

    public string FormatSeparator { get; set; } = ",";

    public int MaximumReplaces { get; set; } = 1000;

    public Func<string, Match, string> MissingValueHandler { get; set; }

    public bool UseFastNestingMatch { get; set; } = true;

    /// <summary>
    ///     Skips the interpolation and nesting of placeholders contained in the inserted values, like the i18next
    ///     <c>skipOnVariables</c> option.
    /// </summary>
    public bool SkipOnVariables { get; set; } = true;

    /// <summary>
    ///     Passes values without a format to the formatters, like the i18next <c>alwaysFormat</c> option.
    /// </summary>
    public bool AlwaysFormat { get; set; }

    public DefaultInterpolator(ILogger logger)
    {
        _logger = logger;
        DefaultFormatter = new DefaultFormatter(_logger);
    }

    public virtual bool CanNest(string source)
    {
        return UseFastNestingMatch ? source.Contains(NestingPrefix) : _nestingRegex.IsMatch(source);
    }

    public List<IFormatter> Formatters => _formatters ??= [];

    public virtual Task<string> InterpolateAsync(string source, string key, string language, IDictionary<string, object> args)
    {
        return Task.FromResult(Interpolate(source, key, language, args));
    }

    public string Interpolate(string source, string key, string language, IDictionary<string, object> args)
    {
        var replaces = 0;
        var result = InterpolateOnce(source, language, args, ref replaces);

        while (!SkipOnVariables && replaces < MaximumReplaces && !ReferenceEquals(result, source))
        {
            source = result;
            result = InterpolateOnce(source, language, args, ref replaces);
        }

        return result;
    }

    private string InterpolateOnce(string source, string language, IDictionary<string, object> args, ref int replaces)
    {
        var start = source.IndexOf(Prefix, StringComparison.Ordinal);

        if (start < 0)
            return source;

        StringBuilder builder = null;
        var position = 0;

        while (start >= 0 && replaces < MaximumReplaces)
        {
            var expressionStart = start + Prefix.Length;
            var end = expressionStart < source.Length ? source.IndexOf(Suffix, expressionStart + 1, StringComparison.Ordinal) : -1;

            if (end < 0)
                break;

            if (source.IndexOf('\n', expressionStart, end - expressionStart) >= 0)
            {
                start = source.IndexOf(Prefix, start + 1, StringComparison.Ordinal);
                continue;
            }

            var expression = source.Substring(expressionStart, end - expressionStart);
            var unescape = expression.Length > UnescapePrefix.Length && expression.StartsWith(UnescapePrefix, StringComparison.Ordinal);

            if (unescape)
                expression = expression.Substring(UnescapePrefix.Length);

            var value = GetValueForExpression(expression, language, args);

            value ??= MissingValueHandler != null ? MissingValueHandler(source, _expressionRegex.Match(source, start)) : string.Empty;

            if (!unescape && EscapeValues)
                value = EscapeValue(value);

            if (builder == null)
            {
                builder = _cachedBuilder ?? new StringBuilder(source.Length + 32);
                _cachedBuilder = null;
            }

            builder.Append(source, position, start - position).Append(value);
            position = end + Suffix.Length;
            replaces++;

            start = source.IndexOf(Prefix, position, StringComparison.Ordinal);
        }

        if (builder == null)
            return source;

        builder.Append(source, position, source.Length - position);

        var result = builder.ToString();

        if (builder.Capacity <= 1024)
        {
            builder.Clear();
            _cachedBuilder = builder;
        }

        return result;
    }

    internal int CountNestings(string source)
    {
        var count = 0;

        for (var index = source.IndexOf(NestingPrefix, StringComparison.Ordinal); index >= 0; index = source.IndexOf(NestingPrefix, index + 1, StringComparison.Ordinal))
            count++;

        return count;
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
        return string.IsNullOrEmpty(value) ? throw new ArgumentNullException(nameof(value)) : value;
    }

    private void UpdateExpressionRegexes()
    {
        _expressionRegex = CreateExpressionRegex(Prefix, Suffix);
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
        var (actualKey, format) = _expressions.GetOrAdd((key, FormatSeparator), static entry =>
        {
            var expression = entry.Expression.Trim();
            var separatorIndex = expression.IndexOf(entry.Separator, StringComparison.Ordinal);

            return separatorIndex < 0
                ? (expression, null)
                : (expression.Substring(0, separatorIndex).Trim(), expression.Substring(separatorIndex + entry.Separator.Length).Trim());
        });

        if (format == null)
        {
            var plainValue = GetValue(actualKey, args);

            return AlwaysFormat && plainValue != null ? Format(plainValue, null, language) : plainValue.ToInvariantString();
        }

        var value = GetValue(actualKey, args);

        format = ApplyFormatParams(actualKey, format, args);

        var formats = SplitChainedFormats(format);

        if (formats == null)
            return Format(value, format, language);

        var result = Format(value, formats[0], language);

        for (var i = 1; i < formats.Count; i++)
            result = Format(result, formats[i], language);

        return result;
    }

    private string ApplyFormatParams(string key, string format, IDictionary<string, object> args)
    {
        if (args == null || !args.TryGetValue("formatParams", out var formatParams) || formatParams == null)
            return format;

        if (!formatParams.ToDictionary().TryGetValue(key, out var keyParams) || keyParams == null)
            return format;

        var options = new List<string>();

        foreach (var option in keyParams.ToDictionary())
            options.Add($"{option.Key}: {Convert.ToString(option.Value, CultureInfo.InvariantCulture)}");

        if (options.Count == 0)
            return format;

        var optionsString = string.Join("; ", options);
        var formats = SplitChainedFormats(format) ?? [format];

        for (var i = 0; i < formats.Count; i++)
        {
            if (!IntlFormatter.IsFormatName(formats[i]))
                continue;

            var end = formats[i].LastIndexOf(')');

            formats[i] = formats[i].IndexOf('(') > 0 && end > 0
                ? $"{formats[i].Substring(0, end)}; {optionsString})"
                : $"{formats[i]}({optionsString})";
        }

        return string.Join(FormatSeparator + " ", formats);
    }

    private List<string> SplitChainedFormats(string format)
    {
        if (format.IndexOf(FormatSeparator, StringComparison.Ordinal) < 0)
            return null;

        var formats = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < format.Length; i++)
        {
            if (format[i] == '(')
                depth++;
            else if (format[i] == ')' && depth > 0)
                depth--;
            else if (depth == 0 && string.CompareOrdinal(format, i, FormatSeparator, 0, FormatSeparator.Length) == 0)
            {
                formats.Add(format.Substring(start, i - start).Trim());
                start = i + FormatSeparator.Length;
                i = start - 1;
            }
        }

        formats.Add(format.Substring(start).Trim());

        foreach (var chainedFormat in formats)
        {
            var optionsIndex = chainedFormat.IndexOf('(');
            var name = (optionsIndex > -1 ? chainedFormat.Substring(0, optionsIndex) : chainedFormat).Trim();

            if (!ChainableFormats.Contains(name))
                return null;
        }

        return formats;
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

        return value.Contains(match.Value) ? source : source.ReplaceFirst(match.Value, value);
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
