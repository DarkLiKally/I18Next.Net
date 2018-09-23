using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using I18Next.Net.Formatters;
using I18Next.Net.Internal;
using Newtonsoft.Json.Linq;

namespace I18Next.Net.Plugins
{
    public class DefaultInterpolator : IInterpolator
    {
        private const string ExpressionPrefix = @"\{\{";
        private const string ExpressionSuffix = @"\}\}";
        private const string NestingPrefix = @"\$t\(";
        private const string NestingPrefixPlain = @"$t(";
        private const string NestingSuffix = @"\)";

        private const string UnescapedExpressionPrefix = @"-";
        private const string UnescapedExpressionSuffix = @"";

        private static readonly Regex ExpressionRegex = new Regex($"{ExpressionPrefix}(.+?){ExpressionSuffix}");

        private static readonly Regex UnescapedExpressionRegex =
            new Regex($"{ExpressionPrefix}{UnescapedExpressionPrefix}(.+?){UnescapedExpressionSuffix}{ExpressionSuffix}");

        private static readonly Regex NestingRegex = new Regex($"{NestingPrefix}(.+?){NestingSuffix}");


        private List<IFormatter> _formatters;

        public IFormatter DefaultFormatter { get; set; } = new DefaultFormatter();

        public bool EscapeValues { get; set; } = true;

        public string FormatSeparator { get; set; } = ",";

        public int MaximumReplaces { get; set; } = 1000;

        public Func<string, Match, string> MissingValueHandler { get; set; }

        public bool UseFastNestingMatch { get; set; } = true;

        public virtual bool CanNest(string source)
        {
            return UseFastNestingMatch ? source.Contains(NestingPrefixPlain) : NestingRegex.IsMatch(source);
        }

        public List<IFormatter> Formatters => _formatters ?? (_formatters = new List<IFormatter>());

        public virtual Task<string> InterpolateAsync(string source, string key, string language, IDictionary<string, object> args)
        {
            var unescapeMatches = UnescapedExpressionRegex.Matches(source);
            var matches = ExpressionRegex.Matches(source);

            var result = source;
            var replaces = 0;

            for (var i = 0; i < unescapeMatches.Count; i++)
            {
                var match = unescapeMatches[i];
                result = HandleUnescapeRegexMatch(result, language, args, match);

                replaces++;

                if (replaces >= MaximumReplaces)
                    break;
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

        public virtual async Task<string> NestAsync(string source, string language, IDictionary<string, object> args,
            TranslateAsyncDelegate translateAsync)
        {
            var matches = NestingRegex.Matches(source);

            var result = source;

            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                result = await HandleNestingRegexMatchAsync(result, language, args, translateAsync, match);
            }

            return result;
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

                if (lastObject is Dictionary<string, object> dict)
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

        protected virtual async Task<string> HandleNestingRegexMatchAsync(string source, string language, IDictionary<string, object> args,
            TranslateAsyncDelegate translateAsync, Match match)
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
                childArgs = await ParseNestedArgsAsync(childArgsString, language, args);
            }

            var value = await translateAsync(language, key, childArgs);

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

        protected virtual async Task<IDictionary<string, object>> ParseNestedArgsAsync(string argsString, string language,
            IDictionary<string, object> parentArgs)
        {
            argsString = await InterpolateAsync(argsString, null, language, parentArgs);
            argsString = argsString.Replace('\'', '"');

            IDictionary<string, object> args = JObject.Parse(argsString).ToObject<Dictionary<string, object>>();

            if (parentArgs != null)
                args = parentArgs.MergeLeft(args);

            return args;
        }
    }
}
