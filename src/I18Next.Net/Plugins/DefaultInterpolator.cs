using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using I18Next.Net.Formatters;
using I18Next.Net.Internal;
using I18Next.Net.Logging;

namespace I18Next.Net.Plugins;

/// <summary>
///     The default <see cref="IInterpolator" /> implementing the i18next interpolation and nesting syntax: variables like
///     <c>{{name}}</c> and <c>{{user.name}}</c>, formats like <c>{{price, currency(EUR)}}</c> or chained
///     <c>{{name, lowercase, uppercase}}</c>, unescaped values like <c>{{- html}}</c> and nested translations like
///     <c>$t(key)</c> or <c>$t(key, { "count": 2 })</c>.
/// </summary>
/// <remarks>
///     Values are formatted by the first of the <see cref="Formatters" /> able to format them, otherwise by the
///     <see cref="DefaultFormatter" />. Values are not escaped by this class; use the <see cref="HtmlInterpolator" /> or
///     override <see cref="EscapeValue" /> to escape them.
/// </remarks>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
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

    /// <summary>
    ///     The prefix of a variable, like the i18next <c>prefix</c> option. Defaults to <c>{{</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is set to null or empty.</exception>
    public string Prefix
    {
        get;
        set
        {
            field = ValidateDelimiter(value);
            UpdateExpressionRegexes();
        }
    } = DefaultPrefix;

    /// <summary>
    ///     The suffix of a variable, like the i18next <c>suffix</c> option. Defaults to <c>}}</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is set to null or empty.</exception>
    public string Suffix
    {
        get;
        set
        {
            field = ValidateDelimiter(value);
            UpdateExpressionRegexes();
        }
    } = DefaultSuffix;

    /// <summary>
    ///     The prefix marking a variable whose value is not escaped, like the i18next <c>unescapePrefix</c> option. Defaults
    ///     to <c>-</c>, e.g. <c>{{- html}}</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is set to null or empty.</exception>
    public string UnescapePrefix
    {
        get;
        set
        {
            field = ValidateDelimiter(value);
            UpdateExpressionRegexes();
        }
    } = DefaultUnescapePrefix;

    /// <summary>
    ///     The prefix of a nested translation, like the i18next <c>nestingPrefix</c> option. Defaults to <c>$t(</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is set to null or empty.</exception>
    public string NestingPrefix
    {
        get;
        set
        {
            field = ValidateDelimiter(value);
            _nestingRegex = CreateExpressionRegex(field, NestingSuffix);
        }
    } = DefaultNestingPrefix;

    /// <summary>
    ///     The suffix of a nested translation, like the i18next <c>nestingSuffix</c> option. Defaults to <c>)</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The value is set to null or empty.</exception>
    public string NestingSuffix
    {
        get;
        set
        {
            field = ValidateDelimiter(value);
            _nestingRegex = CreateExpressionRegex(NestingPrefix, field);
        }
    } = DefaultNestingSuffix;

    /// <summary>
    ///     The formatter used when none of the <see cref="Formatters" /> can format a value. Defaults to a
    ///     <see cref="I18Next.Net.Formatters.DefaultFormatter" />.
    /// </summary>
    public IFormatter DefaultFormatter { get; set; }

    /// <summary>
    ///     The format names which can be chained with the <see cref="FormatSeparator" />, e.g.
    ///     <c>{{value, number, uppercase}}</c>. A format containing other names is passed to the formatters as a whole, so
    ///     format strings containing the separator, like <c>{{date, MMM d, yyyy}}</c>, keep working. Contains the built-in
    ///     formats <c>number</c>, <c>currency</c>, <c>datetime</c>, <c>relativetime</c>, <c>list</c>, <c>lowercase</c> and
    ///     <c>uppercase</c> by default and compares case-insensitively.
    /// </summary>
    public ISet<string> ChainableFormats { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "number", "currency", "datetime", "relativetime", "list", "lowercase", "uppercase"
    };

    /// <summary>
    ///     Whether inserted values are passed to <see cref="EscapeValue" />, like the i18next <c>escapeValue</c> option.
    ///     Enabled by default. Has no effect unless <see cref="EscapeValue" /> is overridden, e.g. by the
    ///     <see cref="HtmlInterpolator" />.
    /// </summary>
    public bool EscapeValues { get; set; } = true;

    /// <summary>
    ///     The separator between a variable and its format and between a nested key and its arguments, like the i18next
    ///     <c>formatSeparator</c> option. Defaults to <c>,</c>.
    /// </summary>
    public string FormatSeparator { get; set; } = ",";

    /// <summary>
    ///     The maximum number of variables replaced in a single interpolation, like the i18next <c>maxReplaces</c> option.
    ///     Protects against endless loops when <see cref="SkipOnVariables" /> is disabled. Defaults to 1000.
    /// </summary>
    public int MaximumReplaces { get; set; } = 1000;

    /// <summary>
    ///     Provides the value of a variable without a value, like the i18next <c>missingInterpolationHandler</c> option. It
    ///     receives the interpolated text and the match of the variable and returns the inserted value. Missing values are
    ///     replaced with an empty string when not set.
    /// </summary>
    public Func<string, Match, string> MissingValueHandler { get; set; }

    /// <summary>
    ///     Whether <see cref="CanNest" /> only checks for the <see cref="NestingPrefix" /> instead of matching the complete
    ///     nesting expression. Enabled by default.
    /// </summary>
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

    /// <summary>
    ///     Creates an interpolator using a <see cref="I18Next.Net.Formatters.DefaultFormatter" /> as
    ///     <see cref="DefaultFormatter" />.
    /// </summary>
    /// <param name="logger">The logger used by the default formatter.</param>
    public DefaultInterpolator(ILogger logger)
    {
        _logger = logger;
        DefaultFormatter = new DefaultFormatter(_logger);
    }

    /// <inheritdoc />
    public virtual bool CanNest(string source)
    {
        return UseFastNestingMatch ? source.Contains(NestingPrefix) : _nestingRegex.IsMatch(source);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     The formatters are asked in order; the first one returning true from <see cref="IFormatter.CanFormat" /> formats
    ///     the value.
    /// </remarks>
    public List<IFormatter> Formatters => _formatters ??= [];

    /// <inheritdoc />
    public virtual Task<string> InterpolateAsync(string source, string key, string language, IDictionary<string, object> args)
    {
        return Task.FromResult(Interpolate(source, key, language, args));
    }

    /// <summary>
    ///     Replaces the variables in the given text with the formatted values of the arguments synchronously.
    /// </summary>
    /// <param name="source">The text containing the variables.</param>
    /// <param name="key">The key of the translation, if any.</param>
    /// <param name="language">The language used to format the values.</param>
    /// <param name="args">
    ///     The arguments providing the values. Nested values of dictionaries and anonymous objects are accessed with dots,
    ///     e.g. <c>{{user.name}}</c>.
    /// </param>
    /// <returns>The interpolated text.</returns>
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

    /// <inheritdoc />
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

    /// <summary>
    ///     Escapes an inserted value when <see cref="EscapeValues" /> is enabled and the variable does not start with the
    ///     <see cref="UnescapePrefix" />. Returns the value unchanged by default.
    /// </summary>
    /// <param name="value">The formatted value.</param>
    /// <returns>The escaped value.</returns>
    protected virtual string EscapeValue(string value)
    {
        return value;
    }

    /// <summary>
    ///     Formats a value with the first of the <see cref="Formatters" /> able to format it, otherwise with the
    ///     <see cref="DefaultFormatter" />.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The format, e.g. <c>number(minimumFractionDigits: 2)</c>, or null if none was given.</param>
    /// <param name="language">The language used to format the value.</param>
    /// <returns>The formatted value.</returns>
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

    /// <summary>
    ///     Gets the value of a variable from the arguments. Dotted keys like <c>user.name</c> access nested dictionaries and
    ///     anonymous objects.
    /// </summary>
    /// <param name="key">The name of the variable.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>The value or null if it does not exist.</returns>
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

    /// <summary>
    ///     Gets the formatted value of a variable expression like <c>price, currency(EUR)</c>. Applies the
    ///     <c>formatParams</c> argument to built-in formats and chains the <see cref="ChainableFormats" />.
    /// </summary>
    /// <param name="key">The expression between the <see cref="Prefix" /> and the <see cref="Suffix" />.</param>
    /// <param name="language">The language used to format the value.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>The formatted value or null if the variable has no value.</returns>
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

    /// <summary>
    ///     Replaces a single nested translation like <c>$t(key)</c> or <c>$t(key, { "count": 2 })</c> with its translation.
    ///     The nested arguments override the arguments of the outer translation. A translation containing the nesting
    ///     expression itself is not inserted, to prevent endless recursion.
    /// </summary>
    /// <param name="source">The text containing the nested translation.</param>
    /// <param name="language">The language used for the nested translation.</param>
    /// <param name="args">The arguments of the outer translation.</param>
    /// <param name="translateAsync">Translates the nested key.</param>
    /// <param name="match">The match of the nested translation.</param>
    /// <returns>The text with the nested translation replaced.</returns>
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

    /// <summary>
    ///     Parses the arguments of a nested translation. Variables in them are interpolated first, single quotes are treated
    ///     as double quotes and the result is parsed as JSON allowing comments and trailing commas.
    /// </summary>
    /// <param name="argsString">The arguments, e.g. <c>{ 'count': {{count}} }</c>.</param>
    /// <param name="language">The language used to interpolate the arguments.</param>
    /// <param name="parentArgs">The arguments of the outer translation, overridden by the parsed arguments.</param>
    /// <returns>The arguments of the nested translation.</returns>
    /// <exception cref="JsonException">The arguments are not valid JSON.</exception>
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
